# AWS OIDC Authentication & Authorization Demo

## Prerequisites
- AWS Account with appropriate permissions
- .NET 9 SDK installed
- AWS CLI installed and configured
- IAM user created (not root user) for CLI access

## Step 0: Install and Configure AWS CLI

### 0.1: Install AWS CLI

1. Download the AWS CLI installer for Windows:
   - Visit: https://aws.amazon.com/cli/
   - Or download directly: https://awscli.amazonaws.com/AWSCLIV2.msi

2. Run the installer (AWSCLIV2.msi) and follow the prompts

3. Verify installation by opening a **new** terminal window and running:
   ```bash
   aws --version
   ```
   You should see output like: `aws-cli/2.x.x Python/3.x.x Windows/...`

### 0.2: Create IAM User If you Don't Have One

⚠️ **Important:** Do NOT use root user access keys!

1. Go to AWS Console → **IAM**
2. Click **Users** → **Create user**
3. Username: `cli-demo-user`
4. Click **Next**
5. Select **Attach policies directly** → Choose **AdministratorAccess** (for demo only)
6. Click **Next** → **Create user**
7. Click on the created user → **Security credentials** tab
8. Click **Create access key**
9. Select **Command Line Interface (CLI)**
10. Check the confirmation box → **Next** → **Create access key**
11. **Copy both values immediately** (you won't see the secret again!)

### 0.3: Configure AWS CLI

1. In your terminal, run:
   ```bash
   aws configure
   ```

2. Enter your IAM user credentials:
   - **AWS Access Key ID**: [from step 0.2]
   - **AWS Secret Access Key**: [from step 0.2]
   - **Default region name**: `us-east-1` (or your preferred region)
   - **Default output format**: `json`

3. Verify configuration:
   ```bash
   aws sts get-caller-identity
   ```
   This should return your IAM user information.

---

## Step 1: Install Required NuGet Packages
- [x] Install the required NuGet packages for JWT Bearer authentication:

```bash
cd c:\Workspaces\oidcDemo\MetricsApiAWS
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```

## Step 2: Create AWS Cognito User Pool

AWS Cognito will be our identity provider for machine-to-machine authentication.

### 2.1: Create the User Pool (Amazone Web UI)

1. Open the AWS Console and navigate to **Amazon Cognito**
2. Click **"Get started for free in less than five minutes"** (left yellow button)
3. **Define your application:**
   - Application type: Select **Machine-to-machine application**
   - Name your application: `MetricsApiUserPool`
   - Options for sign-in identifiers: Check **Email** only
   - Self-registration: **Uncheck** "Enable self-registration"
   - Click **Create**

4. Click **"Go to overview"** button

### 2.2: Collect Important Values (GUI)

From the Overview page, copy these values (you'll need them later):

1. **User Pool ID**: (format: `eu-north-1_8l8fyPnvr7`)
   - Found in the "User pool information" section
   
2. **AWS Region**: (format: `eu-north-1`)
   - Part of the User Pool ID, before the underscore
   
3. **User Pool ARN**: (format: `arn:aws:cognito-idp:eu-north-1:140977286959:userpool/eu-north-1_8l8fyPnvr7`)
   - Found in the "User pool information" section
### 2.2: Collect Important Values (CLI)

1. Run the following command to get the User Pool ID:
```bash
aws cognito-idp list-user-pools --max-results 60
```
2. Save the User Pool ID for step 2.4.

### 2.3: Get the App Client ID

1. In the left sidebar, click **Applications** → **App clients**
2. You should see your app client listed (e.g., `MetricsApiUserPool`)
3. Click on the client name
4. Copy the **Client ID** (format: `7mah0vs17e0t52rug0f18tbd2g`)

### 2.4: Create Resource Server and Custom Scopes

We need to define custom scopes for Reader and Writer roles.

1. In the left sidebar, click **Applications** → **Resource servers**
2. Click **Create resource server**
3. Configure:
   - **Resource server name**: `MetricsApi`
   - **Resource server identifier**: `metrics-api`
   - **Custom scopes**: Add two scopes:
     - Scope name: `read` | Description: `Read test results`
     - Scope name: `write` | Description: `Write and delete test results`
4. Click **Create resource server**

### 2.4 (CLI Alternative): Create Resource Server and Scopes

Set variables (replace with your actual). User Pool ID from step 2.2. Identifier and Name is your choice.
```cmd
set REGION=eu-north-1
set USER_POOL_ID=eu-north-1_8iRyPnvr7 

aws cognito-idp create-resource-server ^
  --region %REGION% ^
  --user-pool-id %USER_POOL_ID% ^
  --identifier metrics-api ^
  --name MetricsApi ^
  --scopes ScopeName=read,ScopeDescription="Read test results" ScopeName=write,ScopeDescription="Write and delete test results"

Verify:
```cmd
aws cognito-idp list-resource-servers --region %REGION% --user-pool-id %USER_POOL_ID% --max-results 50
```
### 2.5: Configure App Client for M2M Authentication

1. Go back to **Applications** → **App clients**
2. Click on your app client (`MetricsApiUserPool`)
3. Scroll to **Hosted UI settings** and configure:
   - **Allowed callback URLs**: `http://localhost` (required but not used for M2M)
   - **Identity providers**: Check **Cognito user pool**
   - **OAuth 2.0 grant types**: Check **Client credentials**
   - **Custom scopes**: Check both `metrics-api/read` and `metrics-api/write`
4. Click **Save changes**
### 2.5 (CLI): Configure App Client for Client Credentials & Scopes

List app clients to get Client ID:
```bash
aws cognito-idp list-user-pool-clients --region $REGION --user-pool-id $USER_POOL_ID --max-results 10
```

Set variable (replace with your real client id):
```bash
CLIENT_ID=7mah0vs17e0t52rug0f18tbd2g
```

Update client:
```bash
aws cognito-idp update-user-pool-client \
  --region $REGION \
  --user-pool-id $USER_POOL_ID \
  --client-id $CLIENT_ID \
  --allowed-o-auth-flows client_credentials \
  --allowed-o-auth-scopes metrics-api/read metrics-api/write \
  --allowed-o-auth-flows-user-pool-client \
  --supported-identity-providers COGNITO
```

Fetch client secret (needed for token requests for demo):
```bash
CLIENT_SECRET=$(aws cognito-idp describe-user-pool-client \
  --region $REGION \
  --user-pool-id $USER_POOL_ID \
  --client-id $CLIENT_ID \
  --query 'UserPoolClient.ClientSecret' --output text)
echo "Client Secret: $CLIENT_SECRET"
```
### 2.6 (Updated): Use Existing User Pool Domain

The user pool already has an auto-generated domain. Find it:

```cmd
set REGION=eu-north-1
set USER_POOL_ID=eu-north-1_8iRyPnvr7

aws cognito-idp describe-user-pool --region %REGION% --user-pool-id %USER_POOL_ID% --query "UserPool.Domain" --output text
```

Result: `eu-north-18irypnvr7`

Set the domain variable:
```cmd
set DOMAIN=eu-north-18irypnvr7
```

Token endpoint format:
```
https://{domain}.auth.{region}.amazoncognito.com/oauth2/token
```

### 2.7: Request Tokens (Updated with correct domain)

```cmd
set CLIENT_ID=7mah0vs17e0t52rug0fi8tbd2g
set CLIENT_SECRET=10kqclcs9n0ea74nrcrlpmo326cgndv220068nmqevspslme0npg
set DOMAIN=eu-north-18irypnvr7
set REGION=eu-north-1

REM Request READ token
curl -X POST ^
  https://%DOMAIN%.auth.%REGION%.amazoncognito.com/oauth2/token ^
  -H "Content-Type: application/x-www-form-urlencoded" ^
  -u %CLIENT_ID%:%CLIENT_SECRET% ^
  -d "grant_type=client_credentials&scope=metrics-api/read"

REM Request WRITE token
curl -X POST ^
  https://%DOMAIN%.auth.%REGION%.amazoncognito.com/oauth2/token ^
  -H "Content-Type: application/x-www-form-urlencoded" ^
  -u %CLIENT_ID%:%CLIENT_SECRET% ^
  -d "grant_type=client_credentials&scope=metrics-api/write"
```

Expected response (JSON):
```json
{
  "access_token": "eyJraWQiOiJ...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "scope": "metrics-api/read"
}
```

Save both tokens (read and write) for testing later.

### 2.8: Values Summary

```
User Pool ID: eu-north-1_8iRyPnvr7
AWS Region: eu-north-1
User Pool Domain: eu-north-18irypnvr7
App Client ID: 7mah0vs17e0t52rug0fi8tbd2g
App Client Secret: 10kqclcs9n0ea74nrcrlpmo326cgndv220068nmqevspslme0npg
Resource Server Identifier: metrics-api
Issuer URL: https://cognito-idp.eu-north-1.amazonaws.com/eu-north-1_8iRyPnvr7
Token Endpoint: https://eu-north-18irypnvr7.auth.eu-north-1.amazoncognito.com/oauth2/token
Scopes: metrics-api/read, metrics-api/write
```

---

## Step 3: Configure JWT Authentication in the .NET API

Now we'll configure the API to validate JWT tokens from Cognito and enforce scope-based authorization.

### 3.1: Update appsettings.json

Add AWS Cognito configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AWS": {
    "Region": "eu-north-1",
    "UserPoolId": "eu-north-1_8iRyPnvr7",
    "AppClientId": "7mah0vs17e0t52rug0fi8tbd2g"
  }
}
```
### 3.2: Update Program.cs

Add JWT authentication and authorization configuration
### 3.3: Update the Controller to Require Authorization

Add authorization attributes to your endpoints:
### 3.4: Test Locally

1. Run the API:
```cmd
cd c:\Workspaces\oidcDemo\MetricsApiAWS
dotnet run
```

2. The API should start on `https://localhost:xxxx` (check console output)

3. Test without token (should get 401 Unauthorized):
```cmd
curl -k https://localhost:7000/api/testresults
```

4. Test with READ token:
```cmd
set READ_TOKEN=eyJraWQiOiJ... (paste your read token here)

curl -k https://localhost:7000/api/testresults ^
  -H "Authorization: Bearer %READ_TOKEN%"
```

5. Test POST with READ token (should get 403 Forbidden):
```cmd
curl -k -X POST https://localhost:7000/api/testresults ^
  -H "Authorization: Bearer %READ_TOKEN%" ^
  -H "Content-Type: application/json" ^
  -d "{\"testName\":\"Test1\",\"outcome\":\"Passed\",\"duration\":1.5}"
```

6. Test POST with WRITE token (should succeed):
```cmd
set WRITE_TOKEN=eyJraWQiOiJ... (paste your write token here)

curl -k -X POST https://localhost:7000/api/testresults ^
  -H "Authorization: Bearer %WRITE_TOKEN%" ^
  -H "Content-Type: application/json" ^
  -d "{\"testName\":\"Test1\",\"outcome\":\"Passed\",\"duration\":1.5}"
```

---
