package com.pharmaco.etl.utils

import java.sql.{Connection, DriverManager, Properties}
import java.util.Properties

object SnowflakeConnector {
  private val url = "jdbc:snowflake://.snowflakecomputing.com"
  private val user = ""
  private val password = ""
  private val database = ""
  private val schema = ""
  private val warehouse = ""
  private val role = ""

  def getConnection(): Connection = {
    val properties = new Properties()
    properties.put("user", user)
    properties.put("password", password)
    properties.put("db", database)
    properties.put("schema", schema)
    properties.put("warehouse", warehouse)
    properties.put("role", role)

    DriverManager.getConnection(url, properties)
  }

  def createTables(): Unit = {
    val connection = getConnection()
    val statement = connection.createStatement()

    val createVariantsTable = """
      CREATE TABLE IF NOT EXISTS ANNOTATED_VARIANTS (
        VARIANT_ID VARCHAR(50),
        CHROMOSOME VARCHAR(10),
        POSITION INTEGER,
        REFERENCE_ALLELE VARCHAR(10),
        ALTERNATE_ALLELE VARCHAR(10),
        GENE VARCHAR(100),
        IMPACT VARCHAR(20),
        CLINICAL_SIGNIFICANCE VARCHAR(50),
        DRUG_INTERACTIONS TEXT,
        RISK_SCORE DOUBLE,
        PROCESSED_AT TIMESTAMP_NTZ,
        UPLOAD_ID VARCHAR(50)
      )
    """

    val createAlternativesTable = """
      CREATE TABLE IF NOT EXISTS DRUG_ALTERNATIVES (
        GENE VARCHAR(100),
        DRUG_INTERACTIONS TEXT,
        CLINICAL_SIGNIFICANCE VARCHAR(50),
        RISK_SCORE DOUBLE,
        ALTERNATIVE_DRUG VARCHAR(100),
        REASON TEXT,
        CONFIDENCE_SCORE DOUBLE,
        CLINICAL_EVIDENCE TEXT,
        DOSAGE_RECOMMENDATION TEXT,
        MONITORING_REQUIREMENTS TEXT,
        CREATED_AT TIMESTAMP_NTZ,
        UPLOAD_ID VARCHAR(50)
      )
    """

    statement.execute(createVariantsTable)
    statement.execute(createAlternativesTable)
    statement.close()
    connection.close()
  }
}
