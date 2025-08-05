
def predict_drug_risk(upload_id, drug_name, variant_count, high_risk_variants, risk_score):
    """
    Predict drug risk for a patient
    
    Args:
        upload_id (str): Patient upload ID
        drug_name (str): Drug name
        variant_count (int): Number of variants
        high_risk_variants (int): Number of high-risk variants
        risk_score (float): Calculated risk score
    
    Returns:
        dict: Prediction results
    """
    import joblib
    import json
    import numpy as np
    
    # load model and scaler
    model = joblib.load('models/drug_risk_model.pkl')
    scaler = joblib.load('models/scaler.pkl')
    
    # load feature columns
    with open('models/feature_columns.json', 'r') as f:
        feature_columns = json.load(f)
    
    # prepare features
    features = {
        'VARIANT_COUNT_NORM': variant_count / 100,  # Normalize
        'HIGH_RISK_RATIO': high_risk_variants / max(variant_count, 1),
        'RISK_SCORE_NORM': risk_score
    }
    
    # add drug features dynamically
    for col in feature_columns:
        if col.startswith('DRUG_'):
            drug_name_from_col = col.replace('DRUG_', '').replace('_', ' ').title()
            features[col] = 1 if drug_name == drug_name_from_col else 0
    
    # Create feature vector
    X = np.array([[features.get(col, 0) for col in feature_columns]])
    
    X_scaled = scaler.transform(X)
    
    # make prediction
    risk_probability = model.predict_proba(X_scaled)[0, 1]
    risk_class = model.predict(X_scaled)[0]
    
    return {
        'upload_id': upload_id,
        'drug_name': drug_name,
        'risk_probability': float(risk_probability),
        'risk_class': int(risk_class),
        'risk_level': 'HIGH_RISK' if risk_class == 1 else 'LOW_RISK',
        'confidence': abs(risk_probability - 0.5) * 2  # Distance from 0.5
    }
