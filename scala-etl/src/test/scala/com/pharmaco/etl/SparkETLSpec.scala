package com.pharmaco.etl

import org.scalatest.funsuite.AnyFunSuite
import org.scalatest.matchers.should.Matchers
import org.apache.spark.sql.{SparkSession, DataFrame}
import org.apache.spark.sql.functions._

class SparkETLSpec extends AnyFunSuite with Matchers {
  var spark: SparkSession = _

  override def beforeAll(): Unit = {
    spark = SparkSession.builder()
      .appName("SparkETLTest")
      .master("local[2]")
      .config("spark.sql.adaptive.enabled", "false")
      .getOrCreate()
  }

  override def afterAll(): Unit = {
    if (spark != null) {
      spark.stop()
    }
  }

  test("processVariants should add variant_id and risk_score") {
    import spark.implicits._

    val testData = Seq(
      ("chr1", 1000, "A", "T", Some("GENE1"), Some("HIGH"), Some("pathogenic"), Some("drug1"))
    ).toDF("chromosome", "position", "reference_allele", "alternate_allele", "gene", "impact", "clinical_significance", "drug_interactions")

    val result = SparkETL.processVariants(testData)

    result.count() shouldBe 1
    result.select("variant_id").first().getString(0) shouldBe "chr1_1000"
    result.select("risk_score").first().getDouble(0) shouldBe 0.9
  }

  test("generateDrugAlternatives should create alternatives for pathogenic variants") {
    import spark.implicits._

    val testData = Seq(
      ("GENE1", "drug1", "pathogenic", 0.9)
    ).toDF("gene", "drug_interactions", "clinical_significance", "risk_score")

    val result = SparkETL.generateDrugAlternatives(testData)

    result.count() shouldBe 1
    result.select("alternative_drug").first().getString(0) shouldBe "Alternative_1"
  }
} 