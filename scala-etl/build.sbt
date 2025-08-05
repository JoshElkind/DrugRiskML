name := "pharmaco-etl"
version := "1.0.0"
scalaVersion := "2.12.15"

libraryDependencies ++= Seq(
  "org.apache.spark" %% "spark-core" % "3.3.0",
  "org.apache.spark" %% "spark-sql" % "3.3.0",
  "org.apache.spark" %% "spark-streaming" % "3.3.0",
  "net.snowflake" % "snowflake-jdbc" % "3.13.33",
  "org.apache.hadoop" % "hadoop-aws" % "3.3.2",
  "com.amazonaws" % "aws-java-sdk-s3" % "1.12.261",
  "org.scalatest" %% "scalatest" % "3.2.12" % Test
)

assemblyMergeStrategy in assembly := {
  case PathList("META-INF", xs @ _*) => MergeStrategy.discard
  case x => MergeStrategy.first
} 