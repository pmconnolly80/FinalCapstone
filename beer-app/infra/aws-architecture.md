# AWS Deployment Plan

## Recommended MVP architecture

- Frontend: static hosting for the React app
- Backend: ASP.NET Core API hosted on AWS App Runner or ECS
- Database: Amazon RDS for PostgreSQL
- Authentication: AWS Cognito or a simple auth provider
- Monitoring: Amazon CloudWatch

## Suggested first deployment shape

- Static site hosting for the frontend
- API service for the backend
- Managed PostgreSQL database
- Environment variables stored securely in AWS

## Notes

This setup is intentionally simple so you can ship quickly while keeping the option to scale later.
