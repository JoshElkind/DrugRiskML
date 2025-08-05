#!/usr/bin/env python3

import pandas as pd
import numpy as np
import xgboost as xgb
from sklearn.model_selection import train_test_split, cross_val_score
from sklearn.metrics import classification_report, roc_auc_score
from sklearn.preprocessing import StandardScaler
from sklearn.ensemble import VotingClassifier, RandomForestClassifier
from sklearn.linear_model import LogisticRegression
import joblib
import json

class XGBoostScikitEnsemble:
    def __init__(self):
        self.ensemble_model = None
        self.xgb_model = None
        self.scaler = StandardScaler()
        self.feature_columns = []
        
    def create_models(self):
        self.xgb_model = xgb.XGBClassifier(
            n_estimators=100,
            max_depth=6,
            learning_rate=0.1,
            random_state=42
        )
        
        rf_model = RandomForestClassifier(
            n_estimators=100,
            max_depth=10,
            random_state=42
        )
        
        lr_model = LogisticRegression(
            random_state=42,
            max_iter=1000
        )
        
        self.ensemble_model = VotingClassifier(
            estimators=[
                ('xgb', self.xgb_model),
                ('rf', rf_model),
                ('lr', lr_model)
            ],
            voting='soft'
        )
    
    def train_models(self, X_train, y_train):
        self.ensemble_model.fit(X_train, y_train)
        self.xgb_model.fit(X_train, y_train)
    
    def evaluate_models(self, X_test, y_test):
        y_pred_ensemble = self.ensemble_model.predict(X_test)
        y_prob_ensemble = self.ensemble_model.predict_proba(X_test)[:, 1]
        
        y_pred_xgb = self.xgb_model.predict(X_test)
        y_prob_xgb = self.xgb_model.predict_proba(X_test)[:, 1]
        
        ensemble_auc = roc_auc_score(y_test, y_prob_ensemble)
        xgb_auc = roc_auc_score(y_test, y_prob_xgb)
        
        return {
            'ensemble_auc': ensemble_auc,
            'xgb_auc': xgb_auc,
            'ensemble_report': classification_report(y_test, y_pred_ensemble),
            'xgb_report': classification_report(y_test, y_pred_xgb)
        }
    
    def save_models(self, results):
        joblib.dump(self.ensemble_model, 'models/ensemble_model.pkl')
        joblib.dump(self.xgb_model, 'models/xgb_model.pkl')
        joblib.dump(self.scaler, 'models/ensemble_scaler.pkl')
        
        metadata = {
            'ensemble_auc': results['ensemble_auc'],
            'xgb_auc': results['xgb_auc'],
            'model_type': 'XGBoost + scikit-learn Ensemble',
            'models': ['XGBoost', 'Random Forest', 'Logistic Regression']
        }
        
        with open('models/ensemble_metadata.json', 'w') as f:
            json.dump(metadata, f, indent=2)

def create_sample_data():
    np.random.seed(42)
    n_samples = 1000
    
    X = np.random.randn(n_samples, 10)
    
    risk_score = (X[:, 0] * 0.3 + X[:, 1] * 0.2 + X[:, 2] * 0.1 + 
                  np.random.normal(0, 0.1, n_samples))
    
    y = (risk_score > np.median(risk_score)).astype(int)
    
    X_train, X_test, y_train, y_test = train_test_split(
        X, y, test_size=0.2, random_state=42, stratify=y
    )
    
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)
    
    return X_train_scaled, X_test_scaled, y_train, y_test, scaler

def main():
    X_train, X_test, y_train, y_test, scaler = create_sample_data()
    
    ensemble = XGBoostScikitEnsemble()
    ensemble.scaler = scaler
    
    ensemble.create_models()
    ensemble.train_models(X_train, y_train)
    results = ensemble.evaluate_models(X_test, y_test)
    ensemble.save_models(results)

if __name__ == "__main__":
    main()