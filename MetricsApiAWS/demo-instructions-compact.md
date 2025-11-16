# OIDC Demo with AWS Lambda + API Gateway + Cognito

## Overview
Demonstrates securing an ASP.NET Core API deployed as AWS Lambda using OpenID Connect (OIDC) authentication via AWS Cognito.

## Prerequisites
- AWS CLI v2 configured with credentials
- .NET 8 SDK
- PowerShell (Windows)

## Resources Created
- **Cognito User Pool:** `eu-north-1_8iRyPnvr7`
- **Resource Server:** `metrics-api` (scopes: `read`, `write`)
- **App Client:** `7mah0vs17e0t52rug0fi8tbd2g`
- **Lambda Function:** `metrics-api-lambda`
- **API Gateway:** `k6opu4nrel` (endpoint: `https://k6opu4nrel.execute-api.eu-north-1.amazonaws.com`)

## Quick Start

### 1. Get Access Tokens
```cmd
set REGION=eu-north-1
set DOMAIN=eu-north-18irypnvr7
set CLIENT_ID=7mah0vs17e0t52rug0fi8tbd2g
set CLIENT_SECRET=10kqclcs9n0ea74nrcrlpmo326cgndv220068nmqevspslme0npg

REM Read token
curl -X POST https://%DOMAIN%.auth.%REGION%.amazoncognito.com/oauth2/token -H "Content-Type: application/x-www-form-urlencoded" -u %CLIENT_ID%:%CLIENT_SECRET% -d "grant_type=client_credentials&scope=metrics-api/read"

REM Write token
curl -X POST https://%DOMAIN%.auth.%REGION%.amazoncognito.com/oauth2/token -H "Content-Type: application/x-www-form-urlencoded" -u %CLIENT_ID%:%CLIENT_SECRET% -d "grant_type=client_credentials&scope=metrics-api/write"
```

### 2. Test API
```cmd
set READ_TOKEN=<paste_access_token_here>
set WRITE_TOKEN=<paste_access_token_here>

REM Test read (401 without token, 200 with READ_TOKEN)
curl -i https://k6opu4nrel.execute-api.eu-north-1.amazonaws.com/api/testresults/summary
curl -i https://k6opu4nrel.execute-api.eu-north-1.amazonaws.com/api/testresults/summary -H "Authorization: Bearer %READ_TOKEN%"

REM Test write (403 with READ_TOKEN, 202 with WRITE_TOKEN)
curl -i -X POST https://k6opu4nrel.execute-api.eu-north-1.amazonaws.com/api/testresults/result -H "Authorization: Bearer %WRITE_TOKEN%" -H "Content-Type: application/json" -d "{\"buildId\":\"123\",\"testName\":\"Test1\",\"outcome\":\"Passed\",\"durationSeconds\":1.5}"
```

## Rebuild & Redeploy

### Update Code
```cmd
cd c:\Workspaces\oidcDemo\MetricsApiAWS
dotnet publish -c Release -o publish
powershell Compress-Archive -Path publish\* -DestinationPath function.zip -Force
aws lambda update-function-code --function-name metrics-api-lambda --zip-file fileb://function.zip --region eu-north-1
```

### View Logs
```cmd
aws logs tail /aws/lambda/metrics-api-lambda --since 5m --region eu-north-1
```

## Key Configuration

### Lambda Environment Variables
- `AWS__Region`: `eu-north-1`
- `AWS__UserPoolId`: `eu-north-1_8iRyPnvr7`

### API Gateway Routes
- `GET /api/testresults/summary` → Lambda (ReadAccess policy)
- `POST /api/testresults/result` → Lambda (WriteAccess policy)

### JWT Validation
- **Issuer:** `https://cognito-idp.eu-north-1.amazonaws.com/eu-north-1_8iRyPnvr7`
- **Audience Validation:** Disabled (client_credentials tokens don't include `aud`)
- **Scope Claim:** Used for authorization policies

## Cleanup
```cmd
REM Delete Lambda
aws lambda delete-function --function-name metrics-api-lambda --region eu-north-1

REM Delete API Gateway
aws apigatewayv2 delete-api --api-id k6opu4nrel --region eu-north-1

REM Delete Cognito domain (if recreating)
aws cognito-idp delete-user-pool-domain --domain eu-north-18irypnvr7 --user-pool-id eu-north-1_8iRyPnvr7 --region eu-north-1
```

## Additional Notes
### Enhancements (Optional Learning)
To expand the demo:

A. Add Swagger/OpenAPI with OAuth2:

Configure Swagger UI to use Cognito token endpoint
Enable "Try it out" with OAuth2 authentication

B. Add User Authentication (in addition to the M2M):

Enable Cognito Hosted UI for user login
Use authorization_code grant instead of client_credentials

C. Deploy with Infrastructure as Code:

Create CloudFormation or Terraform template for reproducible setup

D. Add API Gateway Authorizer:

Use Cognito User Pool authorizer in API Gateway (moves auth validation to gateway layer)