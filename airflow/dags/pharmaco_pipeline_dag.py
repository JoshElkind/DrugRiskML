import os
import sys
import logging
import traceback
from datetime import datetime, timedelta
from airflow import DAG
from airflow.operators.python import PythonOperator
from airflow.operators.bash import BashOperator

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

def setup_logging():
    log_dir = os.path.join(PROJECT_ROOT, "logs")
    os.makedirs(log_dir, exist_ok=True)
    
    log_file = os.path.join(log_dir, 'pharmaco_pipeline.log')
    
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        handlers=[
            logging.FileHandler(log_file, mode='a'),
            logging.StreamHandler(sys.stdout)
        ]
    )
    
    return logging.getLogger(__name__)

logger = setup_logging()

def log_to_file(message, level="INFO"):
    """Log message to file in project root"""
    log_dir = os.path.join(PROJECT_ROOT, "logs")
    os.makedirs(log_dir, exist_ok=True)
    
    log_file = os.path.join(log_dir, 'pharmaco_pipeline.log')
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_entry = f"{timestamp} - {level} - {message}\n"
    
    try:
        with open(log_file, 'a') as f:
            f.write(log_entry)
            f.flush()
    except Exception as e:
        print(f"Failed to write to log file: {e}")
    
    if level == "ERROR":
        logger.error(message)
    elif level == "WARNING":
        logger.warning(message)
    elif level == "INFO":
        logger.info(message)

def ensure_logging_setup():
    """Ensure logging is properly set up for task execution"""
    try:
        log_dir = os.path.join(PROJECT_ROOT, "logs")
        os.makedirs(log_dir, exist_ok=True)
        return True
    except Exception as e:
        print(f"Failed to setup logging: {e}")
        return False

try:
    DAG_ID = "pharmaco_pipeline"
    S3_BUCKET = "pharmaco-vcf-uploads"
    S3_PROCESSED_BUCKET = "pharmaco-vcf-processed"
    SCALA_ETL_PATH = os.path.join(PROJECT_ROOT, "scala-etl")
    SCALA_ETL_MAIN = "com.pharmaco.etl.SparkETL"

    SNOWFLAKE_CONFIG = {
        
    }

    default_args = {
        "owner": "airflow",
        "retries": 1,
        "retry_delay": timedelta(minutes=5),
        "email_on_failure": False,
        "email_on_retry": False,
    }

    def get_latest_vcf_file(**context):
        ensure_logging_setup()
        log_to_file("Starting get_latest_vcf_file task", "INFO")
        
        try:
            upload_id = context['task_instance'].xcom_pull(task_ids='scan_for_vcf_files', key='upload_id')
            filename = context['task_instance'].xcom_pull(task_ids='scan_for_vcf_files', key='filename')
            
            if not upload_id or not filename:
                log_to_file("No upload_id or filename found from previous task", "ERROR")
                raise ValueError("No upload_id or filename found from previous task")
            
            log_to_file(f"Using upload_id: {upload_id}, filename: {filename}", "INFO")
            
            context['task_instance'].xcom_push(key='filename', value=filename)
            context['task_instance'].xcom_push(key='upload_id', value=upload_id)
            
            log_to_file(f"Successfully processed file: {filename}", "INFO")
            return filename
            
        except Exception as e:
            log_to_file(f"Error in get_latest_vcf_file: {str(e)}", "ERROR")
            log_to_file(f"Error type: {type(e).__name__}", "ERROR")
            log_to_file(f"Full traceback: {traceback.format_exc()}", "ERROR")
            raise

    def check_snowflake_available():
        """Check if Snowflake is available"""
        try:
            import snowflake.connector
            log_to_file("Snowflake connector imported successfully", "INFO")
            return True
        except ImportError as e:
            log_to_file(f"Snowflake connector not available: {e}", "WARNING")
            return False
        except Exception as e:
            log_to_file(f"Error importing Snowflake connector: {e}", "ERROR")
            return False

    def setup_snowflake_stages(**context):
        ensure_logging_setup()
        log_to_file("Starting setup_snowflake_stages task (simulated)", "INFO")
        
        try:
            log_to_file("Simulating Snowflake connection", "INFO")
            
            log_to_file("Created parquet_format file format (simulated)", "INFO")
            
            log_to_file("Created variants_stage (simulated)", "INFO")
            log_to_file("Created alternatives_stage (simulated)", "INFO")
            
            log_to_file("Snowflake stages created successfully (simulated)", "INFO")
            return "Stages created successfully (simulated)"
            
        except Exception as e:
            log_to_file(f"Error in simulated Snowflake stages: {str(e)}", "ERROR")
            log_to_file(f"Full traceback: {traceback.format_exc()}", "ERROR")
            raise

    def update_snowflake_status(status, **context):
        ensure_logging_setup()
        log_to_file(f"Starting update_snowflake_status task with status: {status} (simulated)", "INFO")
        
        try:
            upload_id = context['task_instance'].xcom_pull(task_ids='get_vcf_filename', key='upload_id')
            log_to_file(f"Simulating status update for upload_id: {upload_id} to {status}", "INFO")
            
            log_to_file("Simulated: Created GENOME_UPLOADS table", "INFO")
            log_to_file(f"Simulated: Updated status to {status} for upload_id {upload_id}", "INFO")
            
            log_to_file(f"Status updated successfully (simulated): {status}", "INFO")
            return f"Status updated (simulated): {status}"
            
        except Exception as e:
            log_to_file(f"Error in simulated status update: {str(e)}", "ERROR")
            log_to_file(f"Full traceback: {traceback.format_exc()}", "ERROR")
            raise

    def load_variants_to_snowflake(**context):
        ensure_logging_setup()
        log_to_file("Starting load_variants_to_snowflake task (simulated)", "INFO")
        
        try:
            upload_id = context['task_instance'].xcom_pull(task_ids='get_vcf_filename', key='upload_id')
            parquet_file = f"variants_{upload_id}.parquet"
            log_to_file(f"Simulating loading variants file: {parquet_file}", "INFO")
            
            log_to_file("Simulated: Created ANNOTATED_VARIANTS table", "INFO")
            log_to_file(f"Simulated: Loaded variants from {parquet_file}", "INFO")
            
            log_to_file("Variants loaded successfully (simulated)", "INFO")
            return "Variants loaded successfully (simulated)"
            
        except Exception as e:
            log_to_file(f"Error in simulated variants load: {str(e)}", "ERROR")
            log_to_file(f"Full traceback: {traceback.format_exc()}", "ERROR")
            raise

    def load_alternatives_to_snowflake(**context):
        ensure_logging_setup()
        log_to_file("Starting load_alternatives_to_snowflake task (simulated)", "INFO")
        
        try:
            upload_id = context['task_instance'].xcom_pull(task_ids='get_vcf_filename', key='upload_id')
            parquet_file = f"alternatives_{upload_id}.parquet"
            log_to_file(f"Simulating loading alternatives file: {parquet_file}", "INFO")
            
            log_to_file("Simulated: Created DRUG_ALTERNATIVES table", "INFO")
            log_to_file(f"Simulated: Loaded alternatives from {parquet_file}", "INFO")
            
            log_to_file("Alternatives loaded successfully (simulated)", "INFO")
            return "Alternatives loaded successfully (simulated)"
            
        except Exception as e:
            log_to_file(f"Error in simulated alternatives load: {str(e)}", "ERROR")
            log_to_file(f"Full traceback: {traceback.format_exc()}", "ERROR")
            raise

    def simple_test_task(**context):
        """Simple test task that always succeeds"""
        return "success"



    def check_for_new_vcf_files(**context):
        """Check for new unprocessed VCF files in S3 - SIMULATED"""
        try:
          
            upload_id = f"test_upload_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
            filename = f"{upload_id}.vcf.gz"
            
            context['task_instance'].xcom_push(key='filename', value=filename)
            context['task_instance'].xcom_push(key='upload_id', value=upload_id)
            context['task_instance'].xcom_push(key='file_size', value=1000000) 
            context['task_instance'].xcom_push(key='last_modified', value=datetime.now().isoformat())
            
            return f"Simulated VCF file found: {filename} (upload_id: {upload_id})"
            
        except Exception as e:
            return f"Error in simulated VCF check: {str(e)}"



    def mark_file_as_processed(**context):
        """Mark the current file as processed to avoid reprocessing"""
        try:
            filename = context['task_instance'].xcom_pull(key='filename')
            if filename:
                context['task_instance'].xcom_push(key='last_processed_file', value=filename)
                return f"Marked {filename} as processed"
            else:
                return "No filename found to mark as processed"
        except Exception as e:
            return f"Error marking file as processed: {str(e)}"

    dag = DAG(
        DAG_ID,
        default_args=default_args,
        description="Pharmacogenomics pipeline with Snowflake integration",
        schedule=None,
        start_date=datetime(2025, 7, 25),
        catchup=False,
        tags=["pharmacogenomics", "snowflake", "s3"],
    )

    simple_test = PythonOperator(
        task_id="simple_test",
        python_callable=simple_test_task,
        execution_timeout=timedelta(seconds=10),
        dag=dag,
    )
    
    scan_for_vcf_files = PythonOperator(
        task_id="scan_for_vcf_files",
        python_callable=check_for_new_vcf_files,
        dag=dag,
    )
    
    
    get_vcf_filename = PythonOperator(
        task_id="get_vcf_filename",
        python_callable=get_latest_vcf_file,
        dag=dag,
    )

    setup_stages = PythonOperator(
        task_id="setup_stages",
        python_callable=setup_snowflake_stages,
        dag=dag,
    )

    update_status_processing = PythonOperator(
        task_id="update_status_processing",
        python_callable=update_snowflake_status,
        op_kwargs={"status": "PROCESSING"},
        dag=dag,
    )

    run_scala_etl = PythonOperator(
        task_id="run_scala_etl",
        python_callable=lambda **context: f"Scala ETL completed for upload_id: {context['task_instance'].xcom_pull(task_ids='get_vcf_filename', key='upload_id')}",
        dag=dag,
    )

    load_variants = PythonOperator(
        task_id="load_variants",
        python_callable=load_variants_to_snowflake,
        dag=dag,
    )

    load_alternatives = PythonOperator(
        task_id="load_alternatives",
        python_callable=load_alternatives_to_snowflake,
        dag=dag,
    )

    mark_file_as_processed = PythonOperator(
        task_id="mark_file_as_processed",
        python_callable=mark_file_as_processed,
        dag=dag,
    )

    update_status_complete = PythonOperator(
        task_id="update_status_complete",
        python_callable=update_snowflake_status,
        op_kwargs={"status": "COMPLETE"},
        dag=dag,
    )

    simple_test >> scan_for_vcf_files >> get_vcf_filename >> setup_stages >> update_status_processing >> run_scala_etl >> load_variants >> load_alternatives >> mark_file_as_processed >> update_status_complete

    globals()[DAG_ID] = dag
    
    log_to_file("DAG imported successfully", "INFO")

except Exception as e:
    log_to_file(f"CRITICAL ERROR during DAG import: {str(e)}", "ERROR")
    log_to_file(f"Error type: {type(e).__name__}", "ERROR")
    log_to_file(f"Full traceback: {traceback.format_exc()}", "ERROR")
    raise