package com.pharmaco.etl

import org.apache.spark.sql.{SparkSession, DataFrame}
import org.apache.spark.sql.functions._
import org.apache.spark.sql.types._
import java.util.Properties
import java.sql.{Connection, DriverManager}

object SparkETL {
  def main(args: Array[String]): Unit = {
    val spark = SparkSession.builder()
      .appName("PharmacoETL")
      .config("spark.sql.adaptive.enabled", "true")
      .config("spark.sql.adaptive.coalescePartitions.enabled", "true")
      .getOrCreate()

    val uploadId = args(0)
    val s3Bucket = "pharmaco-vcf-uploads"
    val processedBucket = "pharmaco-vcf-processed"

    val vcfData = loadVcfData(spark, s3Bucket, uploadId)
    val processedVariants = processVariants(vcfData)
    val drugAlternatives = generateDrugAlternatives(processedVariants)

    saveToSnowflake(processedVariants, "ANNOTATED_VARIANTS", uploadId)
    saveToSnowflake(drugAlternatives, "DRUG_ALTERNATIVES", uploadId)

    saveToS3(processedVariants, processedBucket, s"variants_${uploadId}.parquet")
    saveToS3(drugAlternatives, processedBucket, s"alternatives_${uploadId}.parquet")

    spark.stop()
  }

  def loadVcfData(spark: SparkSession, bucket: String, uploadId: String): DataFrame = {
    val vcfPath = s"s3a://${bucket}/${uploadId}.vcf.gz"
    
    val schema = StructType(Array(
      StructField("chromosome", StringType, false),
      StructField("position", IntegerType, false),
      StructField("reference_allele", StringType, false),
      StructField("alternate_allele", StringType, false),
      StructField("gene", StringType, true),
      StructField("impact", StringType, true),
      StructField("clinical_significance", StringType, true),
      StructField("drug_interactions", StringType, true)
    ))

    spark.read
      .option("header", "false")
      .option("delimiter", "\t")
      .schema(schema)
      .csv(vcfPath)
  }

  def processVariants(vcfData: DataFrame): DataFrame = {
    vcfData
      .withColumn("variant_id", concat(col("chromosome"), lit("_"), col("position")))
      .withColumn("risk_score", 
        when(col("impact") === "HIGH", 0.9)
        .when(col("impact") === "MODERATE", 0.6)
        .when(col("impact") === "LOW", 0.3)
        .otherwise(0.1))
      .withColumn("processed_at", current_timestamp())
  }

  def generateDrugAlternatives(variants: DataFrame): DataFrame = {
    val drugInteractions = variants
      .filter(col("drug_interactions").isNotNull)
      .select(
        col("gene"),
        col("drug_interactions"),
        col("clinical_significance"),
        col("risk_score")
      )

    drugInteractions
      .withColumn("alternative_drug", 
        when(col("clinical_significance") === "pathogenic", "Alternative_1")
        .when(col("risk_score") > 0.7, "Alternative_2")
        .otherwise("Standard_Treatment"))
      .withColumn("reason", 
        concat(lit("Based on variant in "), col("gene"), lit(" with "), col("clinical_significance")))
      .withColumn("confidence_score", col("risk_score"))
      .withColumn("clinical_evidence", lit("Literature-based recommendation"))
      .withColumn("dosage_recommendation", lit("Adjust based on genetic profile"))
      .withColumn("monitoring_requirements", lit("Enhanced monitoring recommended"))
      .withColumn("created_at", current_timestamp())
  }

  def saveToSnowflake(df: DataFrame, tableName: String, uploadId: String): Unit = {
    val snowflakeOptions = Map(
      "sfURL" -> "",
      "sfUser" -> "",
      "sfPassword" -> "",
      "sfDatabase" -> "",
      "sfSchema" -> "",
      "sfWarehouse" -> "",
      "sfRole" -> ""
    )

    df.write
      .format("net.snowflake.spark.snowflake")
      .options(snowflakeOptions)
      .option("dbtable", tableName)
      .mode("append")
      .save()
  }

  def saveToS3(df: DataFrame, bucket: String, fileName: String): Unit = {
    df.write
      .mode("overwrite")
      .parquet(s"s3a://${bucket}/${fileName}")
  }
} 