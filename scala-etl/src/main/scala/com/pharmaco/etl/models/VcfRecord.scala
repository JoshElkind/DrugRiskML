package com.pharmaco.etl.models

case class VcfRecord(
  chromosome: String,
  position: Int,
  referenceAllele: String,
  alternateAllele: String,
  gene: Option[String],
  impact: Option[String],
  clinicalSignificance: Option[String],
  drugInteractions: Option[String]
)

case class ProcessedVariant(
  variantId: String,
  chromosome: String,
  position: Int,
  referenceAllele: String,
  alternateAllele: String,
  gene: Option[String],
  impact: Option[String],
  clinicalSignificance: Option[String],
  drugInteractions: Option[String],
  riskScore: Double,
  processedAt: java.sql.Timestamp
)

case class DrugAlternative(
  gene: String,
  drugInteractions: String,
  clinicalSignificance: String,
  riskScore: Double,
  alternativeDrug: String,
  reason: String,
  confidenceScore: Double,
  clinicalEvidence: String,
  dosageRecommendation: String,
  monitoringRequirements: String,
  createdAt: java.sql.Timestamp
) 