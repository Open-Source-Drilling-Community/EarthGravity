{{- define "earthgravityservice.name" -}}{{ .Chart.Name | trunc 63 | trimSuffix "-" }}{{- end }}
{{- define "earthgravityservice.fullname" -}}{{ default (printf "%s-%s" .Release.Name .Chart.Name) .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}{{- end }}
{{- define "earthgravityservice.labels" -}}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" }}
app.kubernetes.io/name: {{ include "earthgravityservice.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}
{{- define "earthgravityservice.selectorLabels" -}}
app.kubernetes.io/name: {{ include "earthgravityservice.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}
{{- define "earthgravityservice.serviceAccountName" -}}{{ if .Values.serviceAccount.create }}{{ default (include "earthgravityservice.fullname" .) .Values.serviceAccount.name }}{{ else }}{{ default "default" .Values.serviceAccount.name }}{{ end }}{{- end }}
