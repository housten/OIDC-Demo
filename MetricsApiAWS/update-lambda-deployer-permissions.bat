@echo off
setlocal

:: --- CONFIG ---
set ACCOUNT_ID=140977286959
set REGION=eu-north-1
set ROLE_NAME=GitHubActionsOIDC-Lambda-Deployer
set FUNCTION_NAME=metrics-api-lambda

echo Creating updated policy with Function URL permissions...
>lambda-deploy-policy-updated.json (
  echo {
  echo   "Version": "2012-10-17",
  echo   "Statement": [
  echo     {
  echo       "Effect": "Allow",
  echo       "Action": [
  echo         "lambda:UpdateFunctionCode",
  echo         "lambda:GetFunction",
  echo         "lambda:UpdateFunctionConfiguration",
  echo         "lambda:CreateFunctionUrlConfig",
  echo         "lambda:UpdateFunctionUrlConfig",
  echo         "lambda:GetFunctionUrlConfig",
  echo         "lambda:InvokeFunctionUrl"
  echo       ],
  echo       "Resource": "arn:aws:lambda:%REGION%:%ACCOUNT_ID%:function:%FUNCTION_NAME%"
  echo     }
  echo   ]
  echo }
)

echo Updating inline policy on role %ROLE_NAME% ...
aws iam put-role-policy ^
  --role-name %ROLE_NAME% ^
  --policy-name LambdaDeploymentPolicy ^
  --policy-document file://lambda-deploy-policy-updated.json ^
  --region %REGION%

echo ✅ Policy updated successfully!
echo.
echo Next: Create Function URL with IAM auth
endlocal