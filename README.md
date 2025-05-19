**README**

# MFADemo Project

This project is a web application built using ASP.NET Core. It provides a basic login functionality with MFA (Multi-Factor Authentication) support.

## Prerequisites

* .NET 8.0 or later installed on your machine
* Visual Studio 2022 or later (optional)
* Node.js and npm (for client-side dependencies)
* A database management system (e.g. SQL Server, PostgreSQL, etc.)

## Getting Started

1. Clone the repository:
```bash
git clone https://github.com/ImanJofaris/MFADemo.git
```
2. Navigate to the project directory:
```bash
cd MFADemo
```
3. Restore NuGet packages:
```
dotnet restore
```
4. Build the project:
```
dotnet build
```
5. Run the application:
```
dotnet run
```
6. Open a web browser and navigate to `https://localhost:5001` (or the address specified in the console output)

## Database Migrations

This project uses Entity Framework Core for database operations. To update the database schema, you'll need to run the following commands:

1. Add a new migration:
```
dotnet ef migrations add <migration-name>
```
Replace `<migration-name>` with a descriptive name for the migration (e.g. "InitialCreate").

2. Update the database:
```
dotnet ef database update
```
This will apply the latest migration to the database.

**Note:** Make sure to update the `appsettings.json` file with the correct database connection string before running migrations.

## Client-Side Dependencies

The project uses jQuery and Bootstrap for client-side functionality. These dependencies are included in the `wwwroot/lib` directory.

## MFA Configuration

To enable MFA, you'll need to configure the `MfaSettings` class in the `Models` directory. Please refer to the `MfaSettings.cs` file for more information.

## Troubleshooting

If you encounter any issues during the build or runtime, please check the console output for error messages. You can also try cleaning and rebuilding the project:
```
dotnet clean
dotnet build
```
## Contributing

Contributions are welcome! Please submit pull requests to the main branch.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more information.
