FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /publish

FROM public.ecr.aws/lambda/dotnet:8
WORKDIR /var/task
COPY --from=build /publish .
# Adapt ASP.NET Core to Lambda (package already added to csproj)
CMD ["MetricsApiAWS::MetricsApiAWS.Program::Main"]