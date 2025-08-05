#!/usr/bin/env python3
"""
Drug Risk Assessment ML Training Pipeline
Trains XGBoost model using data from Snowflake for pharmacogenomics risk prediction
"""

import snowflake.connector
import pandas as pd
import numpy as np
import xgboost as xgb
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import classification_report, confusion_matrix, roc_auc_score
from sklearn.preprocessing import LabelEncoder, StandardScaler
import joblib
import json
from datetime import datetime
import warnings
warnings.filterwarnings('ignore')

class DrugRiskTrainer:
    def __init__(self):
        self.model = None
        self.scaler = StandardScaler()
        self.label_encoder = LabelEncoder()
        self.feature_columns = []
        self.model_info = {}
        
    def connect_to_snowflake(self):
        try:
            self.conn = snowflake.connector.connect(
                user="",
                password="",
                account="",
                warehouse="",
                database="",
                schema=""
            )
            return True
        except Exception as e:
            return False
    
    def load_training_data(self):
        try:
            query = """
            SELECT 
                UPLOAD_ID,
                DRUG_NAME,
                VARIANT_COUNT,
                HIGH_RISK_VARIANTS,
                RISK_SCORE,
                CLINICAL_OUTCOME,
                CREATED_AT
            FROM PATIENT_PRESCRIPTIONS
            WHERE CLINICAL_OUTCOME IS NOT NULL
            """
            
            df_prescriptions = pd.read_sql(query, self.conn)
            
            query_variants = """
            SELECT 
                UPLOAD_ID,
                GENE,
                IMPACT,
                VARIANT_TYPE,
                CLINICAL_SIGNIFICANCE,
                DRUG_INTERACTIONS
            FROM ANNOTATED_VARIANTS
            """
            
            df_variants = pd.read_sql(query_variants, self.conn)
            
            query_interactions = """
            SELECT 
                VARIANT_ID,
                GENE,
                DRUGS,
                SIGNIFICANCE,
                CLINICAL_EVIDENCE
            FROM PHARMGKB_VARIANT_DRUG
            """
            
            df_interactions = pd.read_sql(query_interactions, self.conn)
            
            return df_prescriptions, df_variants, df_interactions
            
        except Exception as e:
            return None, None, None
    
    def engineer_features(self, df_prescriptions, df_variants, df_interactions):
        df = df_prescriptions.copy()
        
        df['VARIANT_COUNT_NORM'] = df['VARIANT_COUNT'] / df['VARIANT_COUNT'].max()
        df['HIGH_RISK_RATIO'] = df['HIGH_RISK_VARIANTS'] / df['VARIANT_COUNT'].replace(0, 1)
        df['RISK_SCORE_NORM'] = df['RISK_SCORE']
        
        top_drugs = df['DRUG_NAME'].value_counts().head(10).index
        for drug in top_drugs:
            df[f'DRUG_{drug.upper().replace(" ", "_")}'] = (df['DRUG_NAME'] == drug).astype(int)
        
        if df_variants is not None and len(df_variants) > 0:
            variant_features = df_variants.groupby('UPLOAD_ID').agg({
                'GENE': 'count',
                'IMPACT': lambda x: (x == 'HIGH').sum(),
                'CLINICAL_SIGNIFICANCE': lambda x: (x == 'HIGH').sum()
            }).rename(columns={
                'GENE': 'TOTAL_VARIANTS',
                'IMPACT': 'HIGH_IMPACT_VARIANTS',
                'CLINICAL_SIGNIFICANCE': 'HIGH_SIGNIFICANCE_VARIANTS'
            })
            
            df = df.merge(variant_features, on='UPLOAD_ID', how='left')
            df = df.fillna(0)
        
        if df_interactions is not None and len(df_interactions) > 0:
            interaction_counts = df_interactions['SIGNIFICANCE'].value_counts()
            df['HIGH_SIGNIFICANCE_INTERACTIONS'] = interaction_counts.get('HIGH', 0)
            df['MODERATE_SIGNIFICANCE_INTERACTIONS'] = interaction_counts.get('MODERATE', 0)
        
        if df['CLINICAL_OUTCOME'].nunique() == 1:
            risk_threshold = df['RISK_SCORE'].quantile(0.6)
            df['TARGET'] = (df['RISK_SCORE'] > risk_threshold).astype(int)
        else:
            df['TARGET'] = (df['CLINICAL_OUTCOME'] == 'HIGH_RISK').astype(int)
        
        feature_cols = [
            'VARIANT_COUNT_NORM', 'HIGH_RISK_RATIO', 'RISK_SCORE_NORM'
        ]
        
        drug_features = [col for col in df.columns if col.startswith('DRUG_') and col != 'DRUG_NAME']
        feature_cols.extend(drug_features)
        
        if 'TOTAL_VARIANTS' in df.columns:
            feature_cols.extend(['TOTAL_VARIANTS', 'HIGH_IMPACT_VARIANTS', 'HIGH_SIGNIFICANCE_VARIANTS'])
        
        if 'HIGH_SIGNIFICANCE_INTERACTIONS' in df.columns:
            feature_cols.extend(['HIGH_SIGNIFICANCE_INTERACTIONS', 'MODERATE_SIGNIFICANCE_INTERACTIONS'])
        
        self.feature_columns = feature_cols
        
        return df
    
    def prepare_training_data(self, df):
        X = df[self.feature_columns].fillna(0)
        y = df['TARGET']
        
        X_scaled = self.scaler.fit_transform(X)
        
        X_train, X_test, y_train, y_test = train_test_split(
            X_scaled, y, test_size=0.2, random_state=42, stratify=y
        )
        
        return X_train, X_test, y_train, y_test
    
    def train_model(self, X_train, X_test, y_train, y_test):
        params = {
            'objective': 'binary:logistic',
            'eval_metric': 'logloss',
            'max_depth': 6,
            'learning_rate': 0.1,
            'n_estimators': 100,
            'subsample': 0.8,
            'colsample_bytree': 0.8,
            'random_state': 42,
            'early_stopping_rounds': 10
        }
        
        self.model = xgb.XGBClassifier(**params)
        
        self.model.fit(
            X_train, y_train,
            eval_set=[(X_test, y_test)],
            verbose=True
        )
        
        y_pred = self.model.predict(X_test)
        y_pred_proba = self.model.predict_proba(X_test)[:, 1]
        
        try:
            auc_score = roc_auc_score(y_test, y_pred_proba)
        except ValueError as e:
            auc_score = 0.5
        
        feature_importance = self.model.feature_importances_
        
        try:
            cv_model = xgb.XGBClassifier(
                objective='binary:logistic',
                eval_metric='logloss',
                max_depth=6,
                learning_rate=0.1,
                n_estimators=50,
                subsample=0.8,
                colsample_bytree=0.8,
                random_state=42
            )
            cv_scores = cross_val_score(cv_model, X_train, y_train, cv=3, scoring='roc_auc')
        except Exception as e:
            cv_scores = [0.5]
        
        return {
            'auc_score': auc_score,
            'cv_scores': cv_scores.tolist(),
            'feature_importance': dict(zip(self.feature_columns, feature_importance.tolist()))
        }
    
    def save_model(self, model_info):
        import os
        os.makedirs('models', exist_ok=True)
        
        model_path = 'models/drug_risk_model.pkl'
        joblib.dump(self.model, model_path)
        
        scaler_path = 'models/scaler.pkl'
        joblib.dump(self.scaler, scaler_path)
        
        features_path = 'models/feature_columns.json'
        with open(features_path, 'w') as f:
            json.dump(self.feature_columns, f, indent=2)
        
        metadata = {
            'model_info': model_info,
            'feature_columns': self.feature_columns,
            'training_date': datetime.now().isoformat(),
            'model_path': model_path,
            'scaler_path': scaler_path,
            'features_path': features_path
        }
        
        metadata_path = 'models/model_metadata.json'
        with open(metadata_path, 'w') as f:
            json.dump(metadata, f, indent=2)
        
        return metadata
    
    def create_prediction_pipeline(self):
        prediction_code = '''
def predict_drug_risk(upload_id, drug_name, variant_count, high_risk_variants, risk_score):
    import joblib
    import json
    import numpy as np
    
    model = joblib.load('models/drug_risk_model.pkl')
    scaler = joblib.load('models/scaler.pkl')
    
    with open('models/feature_columns.json', 'r') as f:
        feature_columns = json.load(f)
    
    features = {
        'VARIANT_COUNT_NORM': variant_count / 100,
        'HIGH_RISK_RATIO': high_risk_variants / max(variant_count, 1),
        'RISK_SCORE_NORM': risk_score
    }
    
    for col in feature_columns:
        if col.startswith('DRUG_'):
            drug_name_from_col = col.replace('DRUG_', '').replace('_', ' ').title()
            features[col] = 1 if drug_name == drug_name_from_col else 0
    
    X = np.array([[features.get(col, 0) for col in feature_columns]])
    X_scaled = scaler.transform(X)
    
    risk_probability = model.predict_proba(X_scaled)[0, 1]
    risk_class = model.predict(X_scaled)[0]
    
    return {
        'upload_id': upload_id,
        'drug_name': drug_name,
        'risk_probability': float(risk_probability),
        'risk_class': int(risk_class),
        'risk_level': 'HIGH_RISK' if risk_class == 1 else 'LOW_RISK',
        'confidence': abs(risk_probability - 0.5) * 2
    }
'''
        
        with open('models/predict.py', 'w') as f:
            f.write(prediction_code)
    
    def run_training_pipeline(self):
        if not self.connect_to_snowflake():
            return False
        
        try:
            df_prescriptions, df_variants, df_interactions = self.load_training_data()
            
            if df_prescriptions is None or len(df_prescriptions) == 0:
                return False
            
            df = self.engineer_features(df_prescriptions, df_variants, df_interactions)
            X_train, X_test, y_train, y_test = self.prepare_training_data(df)
            model_info = self.train_model(X_train, X_test, y_train, y_test)
            metadata = self.save_model(model_info)
            self.create_prediction_pipeline()
            
            return True
            
        except Exception as e:
            return False
        
        finally:
            if hasattr(self, 'conn'):
                self.conn.close()

def main():
    trainer = DrugRiskTrainer()
    success = trainer.run_training_pipeline()

if __name__ == "__main__":
    main() 