{{/*
Expand the name of the chart.
*/}}
{{- define "steward.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
Truncates at 63 chars because some Kubernetes name fields are limited to this.
*/}}
{{- define "steward.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart label value.
*/}}
{{- define "steward.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels.
*/}}
{{- define "steward.labels" -}}
helm.sh/chart: {{ include "steward.chart" . }}
{{ include "steward.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels.
*/}}
{{- define "steward.selectorLabels" -}}
app.kubernetes.io/name: {{ include "steward.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Component name helpers.
*/}}
{{- define "steward.api.name" -}}
{{- printf "%s-api" (include "steward.fullname" .) }}
{{- end }}

{{- define "steward.web.name" -}}
{{- printf "%s-web" (include "steward.fullname" .) }}
{{- end }}

{{- define "steward.database.name" -}}
{{- printf "%s-database" (include "steward.fullname" .) }}
{{- end }}

{{/*
PGO credential secret name: <cluster>-pguser-<username>
*/}}
{{- define "steward.database.credentialSecret" -}}
{{- printf "%s-pguser-%s" (include "steward.database.name" .) .Values.database.username }}
{{- end }}

{{/*
Init container that waits for the database to accept connections.
Polls pg_isready until the database is ready before the main container starts.
*/}}
{{- define "steward.initContainers.waitForDatabase" -}}
- name: wait-for-database
  image: postgres:17-alpine
  command:
    - sh
    - -c
    - |
      until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER"; do
        echo "Waiting for database..."
        sleep 2
      done
  env:
    - name: DB_HOST
      valueFrom:
        secretKeyRef:
          name: {{ include "steward.database.credentialSecret" . }}
          key: host
    - name: DB_PORT
      valueFrom:
        secretKeyRef:
          name: {{ include "steward.database.credentialSecret" . }}
          key: port
    - name: DB_USER
      valueFrom:
        secretKeyRef:
          name: {{ include "steward.database.credentialSecret" . }}
          key: user
{{- end }}

{{/*
Common database env vars sourced from the PGO credential secret.
*/}}
{{- define "steward.database.envVars" -}}
- name: DB_HOST
  valueFrom:
    secretKeyRef:
      name: {{ include "steward.database.credentialSecret" . }}
      key: host
- name: DB_PORT
  valueFrom:
    secretKeyRef:
      name: {{ include "steward.database.credentialSecret" . }}
      key: port
- name: DB_NAME
  valueFrom:
    secretKeyRef:
      name: {{ include "steward.database.credentialSecret" . }}
      key: dbname
- name: DB_USER
  valueFrom:
    secretKeyRef:
      name: {{ include "steward.database.credentialSecret" . }}
      key: user
- name: DB_PASSWORD
  valueFrom:
    secretKeyRef:
      name: {{ include "steward.database.credentialSecret" . }}
      key: password
- name: ConnectionStrings__DefaultConnection
  value: "Host=$(DB_HOST);Port=$(DB_PORT);Database=$(DB_NAME);Username=$(DB_USER);Password=$(DB_PASSWORD)"
{{- end }}
