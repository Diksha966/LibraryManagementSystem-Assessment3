Library Management System

Project Overview

The Library Management System is a web-based ASP.NET Core application designed to support the daily operations of a library. The system provides separate functionality for administrators, reception staff and managers, while also providing a public library catalogue that can be accessed without authentication.

The application uses object-oriented inheritance for different library item types, role-based authentication and authorization for staff functions, Entity Framework Core with SQLite for data persistence, and a responsive web interface for public users.

Main Features

Administrator

Administrators can manage the library collection.

Admin functionality includes:

Create library items

View library items

View item details

Edit library items

Delete library items

Manage Books

Manage Toys

Manage Music

Update item status

The item model uses inheritance:

Item
├── Book
├── Toy
└── Music

Reception

Reception staff manage borrowers and borrowing activities.

Reception functionality includes:

Create borrowers

View borrowers

View borrower details

Edit borrowers

Delete borrowers

Borrow items using a library code

Select borrower details

Return borrowed items

Automatically calculate fines for late returns

View fines

Mark fines as paid

When an item is borrowed, its status changes from Available to Borrowed.

When an item is returned, its status changes back to Available.

Late returns automatically generate a fine of:

$2.00 per late day

Manager

Managers have access to a dashboard containing library statistics.

The dashboard displays:

Borrowing Statistics

Total loans

Active loans

Returned loans

Overdue loans

Item Status Statistics

Total items

Available items

Borrowed items

Damaged items

Destroyed items

Fine Statistics

Total fines

Paid fines

Unpaid fines

Total fine amount

Unpaid fine amount

Overdue loans are calculated using the actual due date as well as the stored loan status.

Public Library Portal

The public portal does not require authentication.

Public users can:

Search the library catalogue

Search by title

Search by library code

View item status

View item details

Access the catalogue from mobile devices

The public portal uses a responsive layout so that the interface adapts to smaller screen sizes.

Item Types

Item

Item is the abstract superclass for all library resources.

Common properties:

Id

Title

Description

LibraryCode

Status

Item statuses are:

Available

Borrowed

Damaged

Destroyed

Book

Book inherits from Item.

Additional properties:

Author

Genre

ISBN

Inheritance relationship:

Item
└── Book

Toy

Toy inherits from Item.

Additional properties:

ToyType

RecommendedAge

Inheritance relationship:

Item
└── Toy

Music

Music inherits from Item.

Additional properties:

Artist

Year

Format

Inheritance relationship:

Item
└── Music

User Roles

The application uses ASP.NET Core Identity and role-based authorization.

There are three staff roles:

Role

Main Responsibility

Admin

Manage library items

Reception

Manage borrowers, loans and fines

Manager

View management statistics

The public catalogue is available without authentication.

Role Protection

Controllers are protected using role-based authorization.

For example:

[Authorize(Roles = "Admin")]

is used for administrator functionality.

Similarly:

[Authorize(Roles = "Reception")]

protects reception functionality, while:

[Authorize(Roles = "Manager")]

protects the manager dashboard.

This prevents users from accessing functions outside their assigned role.

Demo Accounts

The application creates the following demonstration accounts when it starts.

Admin

Email: admin@library.com
Password: Admin123!
Role: Admin

Reception

Email: reception@library.com
Password: Reception123!
Role: Reception

Manager

Email: manager@library.com
Password: Manager123!
Role: Manager

These accounts are intended for demonstration and assessment purposes.

Technologies Used

C#

ASP.NET Core

.NET 10

Entity Framework Core

SQLite

ASP.NET Core Identity

Bootstrap

Razor Views

HTML

CSS

Application Architecture

The application follows the ASP.NET Core MVC architecture.

User
 │
 ▼
Controller
 │
 ├── Model / ViewModel
 │
 ▼
Entity Framework Core
 │
 ▼
SQLite Database
 │
 ▼
View
 │
 ▼
User

Controllers

Controllers handle HTTP requests and coordinate application operations.

Main controllers include:

HomeController

ItemsController

BorrowersController

LoansController

FinesController

ManagerController

PublicController

Models

The Models folder contains the main domain classes:

Item

Book

Toy

Music

Borrower

Loan

Fine

ItemStatus

LoanStatus

Data

LibraryDbContext provides the Entity Framework Core database context.

The project uses SQLite for local data storage.

Views

Razor views provide the user interface for the different controllers.

Database

The application uses SQLite.

The database file is:

library.db

Entity Framework Core migrations are included in the project.

The migrations allow the database structure to be created and updated consistently.

How to Run the Application

Prerequisites

Install:

.NET 10 SDK

Visual Studio Code or another compatible C# development environment

Step 1 — Open the project

Open the following folder:

Project/LibraryManagementSystem

Step 2 — Restore dependencies

Open a terminal in the project folder and run:

dotnet restore

Step 3 — Build the project

Run:

dotnet build

The project should build successfully.

Step 4 — Run the application

Run:

dotnet run

The application will display a local address similar to:

http://localhost:5189

Open that address in a web browser.

Database Migrations

If the database needs to be created or updated, run:

dotnet ef database update

The project contains the required Entity Framework Core migrations.

Demonstration Workflow

Admin Demonstration

Log in using the Admin account.

Open Library Items.

Create or view a Book, Toy or Music item.

Edit an item.

View item details.

Delete an item where appropriate.

Change an item's status when required.

Reception Demonstration

Log in using the Reception account.

Open Borrowers.

Create or edit borrower information.

Open Loans.

Create a loan using a library code.

Select the borrower.

Confirm that the item becomes Borrowed.

Return the item.

Confirm that the item becomes Available again.

For a late return, confirm that a fine is automatically created.

Open Fines.

Mark the fine as paid.

Manager Demonstration

Log in using the Manager account.

Open Manager Dashboard.

Review borrowing statistics.

Review item status statistics.

Review fine statistics.

Verify that overdue loans are calculated using their due dates.

Public Demonstration

Log out.

Open Public Library.

Search by title.

Search by library code.

Open an item's public details.

Test the interface on a mobile-sized screen.

Security and Authorization

Staff functionality is protected using authentication and role-based authorization.

Examples of access restrictions include:

Admin users can access item management functions.

Reception users can access borrower, loan and fine functions.

Manager users can access management statistics.

Users cannot access functions outside their assigned role.

Public users can access the public catalogue without authentication.

Public users cannot access staff-only management functions.

Forms that modify data use anti-forgery protection where appropriate.

Validation and Error Handling

The application uses ASP.NET Core model validation and data annotations.

Examples include:

Required fields

String length restrictions

Age range validation

Music year validation

Loan date validation

The application also validates borrowing operations by checking:

A library code has been provided.

The library code identifies an existing item.

The item is currently available.

A valid borrower has been selected.

Invalid requests return appropriate responses such as NotFound or validation messages.

Testing Performed

The application has been tested across the main assessment workflows.

Authentication and Authorization

Admin login tested

Reception login tested

Manager login tested

Role-restricted pages tested

Unauthorized access tested

Borrowing

Library code lookup tested

Case-insensitive library code lookup tested

Available item borrowing tested

Item status change tested

Borrower selection tested

Returns

Item return tested

Returned date recorded

Item status restored to Available

Fines

Late return tested

Fine automatically generated

Fine amount calculated

Fine marked as paid

Manager fine statistics updated

Manager Dashboard

Total loans tested

Active loans tested

Returned loans tested

Overdue calculation tested

Item status statistics tested

Fine statistics tested

Public Portal

Title search tested

Library code search tested

Public details tested without authentication

Responsive mobile layout tested

Mobile navigation tested

Build

The application has been successfully compiled using:

dotnet build

Project Structure

LibraryManagementSystem
│
├── Controllers
│   ├── BorrowersController.cs
│   ├── FinesController.cs
│   ├── HomeController.cs
│   ├── ItemsController.cs
│   ├── LoansController.cs
│   ├── ManagerController.cs
│   └── PublicController.cs
│
├── Data
│   └── LibraryDbContext.cs
│
├── Migrations
│
├── Models
│   ├── Book.cs
│   ├── Borrower.cs
│   ├── Fine.cs
│   ├── Item.cs
│   ├── ItemStatus.cs
│   ├── Loan.cs
│   ├── LoanStatus.cs
│   ├── Music.cs
│   └── Toy.cs
│
├── Services
│
├── ViewModels
│
├── Views
│
├── wwwroot
│
├── Properties
│   └── launchSettings.json
│
├── appsettings.json
├── appsettings.Development.json
├── library.db
├── LibraryManagementSystem.csproj
├── Program.cs
└── README.md

Design Summary

The system uses inheritance to avoid duplicating common item properties across different library resource types. The abstract Item class provides shared properties while Book, Toy and Music provide specialised information.

Role-based authorization separates the responsibilities of administrators, reception staff and managers.

The borrowing workflow connects borrowers with items through loans. When an item is borrowed, its status changes to Borrowed. When it is returned, its status changes to Available. Late returns can automatically generate associated fines.

The manager dashboard provides statistics covering borrowing activity, item status and fines.

The public portal provides a separate catalogue experience so that members of the public can search the collection and view item information without requiring staff authentication.

The application combines these components into a single library management workflow while maintaining separation between public and staff functionality.

Academic Integrity and AI Usage

ChatGPT was used during the development of this project as a supporting learning and development resource.


Examples of AI-assisted activities included:

- Troubleshooting compilation errors and warnings
- Understanding ASP.NET Core MVC concepts
- Reviewing and improving model design
- Understanding Entity Framework Core and database relationships
- Supporting debugging and error resolution
- Reviewing documentation and README structure
- Explaining programming concepts during development

All generated suggestions were reviewed, adapted and tested within the project. The final implementation was tested by the developer to ensure that the application runs correctly and satisfies the assessment requirements.

AI assistance was used as a development and learning aid rather than as a replacement for understanding or testing the implementation.