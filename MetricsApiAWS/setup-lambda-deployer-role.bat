@echo off
setlocal

:: --- CONFIG ---
set ACCOUNT_ID=140977286959
set REGION=eu-north-1
set REPO=housten/oidcDemo
set ROLE_NAME=GitHubActionsOIDC-Lambda-Deployer
set FUNCTION_NAME=metrics-api-lambda

echo Creating trust policy file...
>trust-policy.json (
  echo {
  echo   "Version": "2012-10-17",
  echo   "Statement": [
  echo     {
  echo       "Effect": "Allow",
  echo       "Principal": {
  echo         "Federated": "arn:aws:iam::%ACCOUNT_ID%:oidc-provider/token.actions.githubusercontent.com"
  echo       },
  echo       "Action": "sts:AssumeRoleWithWebIdentity",
  echo       "Condition": {
  echo         "StringEquals": {
  echo           "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
  echo         },
  echo         "StringLike": {
  echo           "token.actions.githubusercontent.com:sub": "repo:%REPO%:*"
  echo         }
  echo       }
  echo     }
  echo   ]
  echo }
)

echo Creating inline policy file...
>lambda-deploy-policy.json (
  echo {
  echo   "Version": "2012-10-17",
  echo   "Statement": [
  echo     {
  echo       "Effect": "Allow",
  echo       "Action": [
  echo         "lambda:UpdateFunctionCode",
  echo         "lambda:GetFunction",
  echo         "lambda:UpdateFunctionConfiguration"
  echo       ],
  echo       "Resource": "arn:aws:lambda:%REGION%:%ACCOUNT_ID%:function:%FUNCTION_NAME%"
  echo     }
  echo   ]
  echo }
)

echo Checking if role already exists...
for /f "delims=" %%R in ('aws iam get-role --role-name %ROLE_NAME% --query "Role.RoleName" --output text 2^>nul') do set EXISTS=%%R

if "%EXISTS%"=="%ROLE_NAME%" (
  echo Role %ROLE_NAME% already exists. Skipping creation.
) else (
  echo Creating role %ROLE_NAME% ...
  aws iam create-role ^
    --role-name %ROLE_NAME% ^
    --assume-role-policy-document file://trust-policy.json ^
    --description "GitHub Actions OIDC deployer for Lambda" ^
    --region %REGION%
)

echo Attaching inline policy...
aws iam put-role-policy ^
  --role-name %ROLE_NAME% ^
  --policy-name LambdaDeploymentPolicy ^
  --policy-document file://lambda-deploy-policy.json ^
  --region %REGION%

echo Retrieving role ARN...
aws iam get-role --role-name %ROLE_NAME% --query "Role.Arn" --output text --region %REGION%

echo Done.
endlocal