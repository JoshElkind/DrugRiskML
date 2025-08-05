FROM apache/airflow:2.8.4-python3.11

#  x86_64 
ARG TARGETPLATFORM
ARG BUILDPLATFORM
RUN echo "I am running on $BUILDPLATFORM"

ENV DOCKER_DEFAULT_PLATFORM=linux/amd64

USER root
RUN apt-get update && apt-get install -y \
    build-essential \
    curl \
    && rm -rf /var/lib/apt/lists/*

USER airflow

RUN pip install --no-cache-dir \
    snowflake-connector-python==3.16.0 \
    snowflake-sqlalchemy==1.7.6 \
    apache-airflow-providers-snowflake==5.4.0 \
    pandas==2.2.1 \
    pyarrow==21.0.0 \
    xgboost==2.0.3 \
    scikit-learn==1.4.0 \
    joblib==1.3.2 \
    numpy==1.26.4

RUN pip install --upgrade apache-airflow==2.8.4

ENV SNOWFLAKE_ACCOUNT=
ENV SNOWFLAKE_USER=
ENV SNOWFLAKE_PASSWORD=
ENV SNOWFLAKE_WAREHOUSE=
ENV SNOWFLAKE_DB=
ENV SNOWFLAKE_SCHEMA=
ENV SNOWFLAKE_ROLE=

RUN mkdir -p /opt/airflow/dags /opt/airflow/logs /opt/airflow/plugins /opt/airflow/data /opt/airflow/config

USER root
RUN chown -R airflow:root /opt/airflow
USER airflow

RUN airflow version

