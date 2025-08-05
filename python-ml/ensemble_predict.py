#!/usr/bin/env python3
"""
Ensemble Prediction Script
Can use both ensemble model and individual XGBoost model for predictions
"""

import joblib
import json
import numpy as np
import pandas as pd
from typing import Dict, List, Tuple

class EnsemblePredictor:
    def __init__(self, model_path: str = 'models/'):
        """Initialize predictor with trained models"""
        self.model_path = model_path
        self.ensemble_model = None
        self.xgb_model = None
        self.scaler = None
        self.feature_columns = []
        
        self.load_models()
    
    def load_models(self):
        """Load trained models"""
        try:
            print("📂 Loading models...")
            
            # Load ensemble model
            self.ensemble_model = joblib.load(f'{self.model_path}ensemble_model.pkl')
            print("✅ Loaded ensemble model")
            
            # Load XGBoost model
            self.xgb_model = joblib.load(f'{self.model_path}xgb_model.pkl')
            print("✅ Loaded XGBoost model")
            
            # Load scaler
            self.scaler = joblib.load(f'{self.model_path}ensemble_scaler.pkl')
            print("✅ Loaded scaler")
            
            # Load feature columns
            with open(f'{self.model_path}ensemble_metadata.json', 'r') as f:
                metadata = json.load(f)
                print(f"✅ Loaded metadata: {metadata['model_type']}")
            
        except Exception as e:
            print(f"❌ Error loading models: {e}")
            print("💡 Make sure to run the training script first")
    
    def preprocess_features(self, features: Dict) -> np.ndarray:
        """Preprocess input features"""
        # Convert features to array
        feature_array = np.array([[
            features.get('variant_count', 0),
            features.get('high_risk_variants', 0),
            features.get('risk_score', 0.5),
            features.get('drug_risk_ratio', 0),
            features.get('variant_density', 0),
            features.get('unique_genes', 0),
            features.get('high_impact_variants', 0),
            features.get('pathogenic_variants', 0),
            features.get('drug_interactions', 0),
            features.get('high_significance_interactions', 0)
        ]])
        
        # Scale features
        if self.scaler:
            feature_array = self.scaler.transform(feature_array)
        
        return feature_array
    
    def predict_ensemble(self, features: Dict) -> Dict:
        """Make prediction using ensemble model"""
        try:
            X = self.preprocess_features(features)
            
            # Get ensemble prediction
            prediction = self.ensemble_model.predict(X)[0]
            probability = self.ensemble_model.predict_proba(X)[0][1]
            
            return {
                'prediction': int(prediction),
                'probability': float(probability),
                'risk_level': self.get_risk_level(probability),
                'model_type': 'Ensemble (XGBoost + scikit-learn)',
                'confidence': self.get_confidence(probability)
            }
            
        except Exception as e:
            print(f"❌ Error in ensemble prediction: {e}")
            return None
    
    def predict_xgb(self, features: Dict) -> Dict:
        """Make prediction using XGBoost model only"""
        try:
            X = self.preprocess_features(features)
            
            # Get XGBoost prediction
            prediction = self.xgb_model.predict(X)[0]
            probability = self.xgb_model.predict_proba(X)[0][1]
            
            # Get feature importance (XGBoost specific)
            feature_importance = self.xgb_model.feature_importances_
            
            return {
                'prediction': int(prediction),
                'probability': float(probability),
                'risk_level': self.get_risk_level(probability),
                'model_type': 'XGBoost',
                'confidence': self.get_confidence(probability),
                'feature_importance': feature_importance.tolist()
            }
            
        except Exception as e:
            print(f"❌ Error in XGBoost prediction: {e}")
            return None
    
    def predict_both(self, features: Dict) -> Dict:
        """Make predictions using both models"""
        ensemble_result = self.predict_ensemble(features)
        xgb_result = self.predict_xgb(features)
        
        if ensemble_result and xgb_result:
            return {
                'ensemble': ensemble_result,
                'xgb': xgb_result,
                'agreement': ensemble_result['prediction'] == xgb_result['prediction'],
                'probability_difference': abs(ensemble_result['probability'] - xgb_result['probability'])
            }
        else:
            return None
    
    def get_risk_level(self, probability: float) -> str:
        """Convert probability to risk level"""
        if probability >= 0.7:
            return 'HIGH'
        elif probability >= 0.4:
            return 'MODERATE'
        else:
            return 'LOW'
    
    def get_confidence(self, probability: float) -> str:
        """Get confidence level based on probability"""
        if probability >= 0.8 or probability <= 0.2:
            return 'HIGH'
        elif probability >= 0.6 or probability <= 0.4:
            return 'MEDIUM'
        else:
            return 'LOW'
    
    def explain_prediction(self, features: Dict, model_type: str = 'ensemble') -> Dict:
        """Generate explanation for prediction"""
        if model_type == 'ensemble':
            result = self.predict_ensemble(features)
        else:
            result = self.predict_xgb(features)
        
        if not result:
            return None
        
        # Create explanation
        explanation = {
            'risk_score': result['probability'],
            'risk_level': result['risk_level'],
            'confidence': result['confidence'],
            'key_factors': self.get_key_factors(features, result),
            'recommendations': self.get_recommendations(result)
        }
        
        return explanation
    
    def get_key_factors(self, features: Dict, result: Dict) -> List[str]:
        """Identify key factors influencing the prediction"""
        factors = []
        
        if features.get('high_risk_variants', 0) > 5:
            factors.append("High number of high-risk genetic variants")
        
        if features.get('pathogenic_variants', 0) > 2:
            factors.append("Multiple pathogenic variants detected")
        
        if features.get('drug_interactions', 0) > 10:
            factors.append("Significant drug-gene interactions")
        
        if result['risk_level'] == 'HIGH':
            factors.append("Overall high genetic risk profile")
        elif result['risk_level'] == 'LOW':
            factors.append("Favorable genetic profile")
        
        return factors
    
    def get_recommendations(self, result: Dict) -> List[str]:
        """Generate recommendations based on prediction"""
        recommendations = []
        
        if result['risk_level'] == 'HIGH':
            recommendations.extend([
                "Consider alternative medications",
                "Monitor closely for adverse reactions",
                "Start with lower dosage",
                "Consult with pharmacogenomics specialist"
            ])
        elif result['risk_level'] == 'MODERATE':
            recommendations.extend([
                "Standard dosing with monitoring",
                "Watch for early signs of adverse reactions",
                "Consider genetic testing for specific genes"
            ])
        else:
            recommendations.extend([
                "Standard dosing protocol",
                "Routine monitoring",
                "Consider standard care guidelines"
            ])
        
        return recommendations

def main():
    """Test the ensemble predictor"""
    print("🧪 Testing Ensemble Predictor")
    print("=" * 40)
    
    # Initialize predictor
    predictor = EnsemblePredictor()
    
    # Test features
    test_features = {
        'variant_count': 15,
        'high_risk_variants': 8,
        'risk_score': 0.75,
        'drug_risk_ratio': 0.53,
        'variant_density': 0.015,
        'unique_genes': 5,
        'high_impact_variants': 3,
        'pathogenic_variants': 2,
        'drug_interactions': 12,
        'high_significance_interactions': 4
    }
    
    print("📊 Test Features:")
    for key, value in test_features.items():
        print(f"  {key}: {value}")
    
    print("\n🔮 Making predictions...")
    
    # Test ensemble prediction
    ensemble_result = predictor.predict_ensemble(test_features)
    if ensemble_result:
        print(f"\n📈 Ensemble Prediction:")
        print(f"  Risk Level: {ensemble_result['risk_level']}")
        print(f"  Probability: {ensemble_result['probability']:.4f}")
        print(f"  Confidence: {ensemble_result['confidence']}")
    
    # Test XGBoost prediction
    xgb_result = predictor.predict_xgb(test_features)
    if xgb_result:
        print(f"\n🌳 XGBoost Prediction:")
        print(f"  Risk Level: {xgb_result['risk_level']}")
        print(f"  Probability: {xgb_result['probability']:.4f}")
        print(f"  Confidence: {xgb_result['confidence']}")
    
    # Test both predictions
    both_result = predictor.predict_both(test_features)
    if both_result:
        print(f"\n🤝 Model Agreement:")
        print(f"  Models agree: {both_result['agreement']}")
        print(f"  Probability difference: {both_result['probability_difference']:.4f}")
    
    # Test explanation
    explanation = predictor.explain_prediction(test_features)
    if explanation:
        print(f"\n📝 Explanation:")
        print(f"  Key Factors: {', '.join(explanation['key_factors'])}")
        print(f"  Recommendations: {', '.join(explanation['recommendations'][:2])}")

if __name__ == "__main__":
    main() 