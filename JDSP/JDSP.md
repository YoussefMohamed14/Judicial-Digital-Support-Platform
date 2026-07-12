# Judicial Digital Support Platform (JDSP)

JDSP is a web-based judicial support platform built with ASP.NET Core MVC.

The system connects clients with verified lawyers and helps court employees manage official cases, lawyer approvals, identity-change requests, hearings, messages, payments, balances, and bilingual English/Arabic user interfaces.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Main Roles](#main-roles)
- [Core Features](#core-features)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Database Setup](#database-setup)
- [Seeded Development Accounts](#seeded-development-accounts)
- [Main Workflows](#main-workflows)
- [File Uploads](#file-uploads)
- [Localization](#localization)
- [Payments and Lawyer Balance](#payments-and-lawyer-balance)
- [Hearing Schedule Logic](#hearing-schedule-logic)
- [Important Development Notes](#important-development-notes)
- [Troubleshooting](#troubleshooting)
- [Recommended Future Refactor](#recommended-future-refactor)
- [Team Testing Checklist](#team-testing-checklist)

---

## Project Overview

JDSP is designed to simplify digital legal support by organizing communication between clients, lawyers, court employees, and super admins.

The platform allows clients to browse verified lawyers, send legal service requests, upload case files, chat with assigned lawyers, receive payment requests, and follow hearing schedules.

Lawyers can manage direct/public requests, communicate with clients, create payment requests, receive payments in their balance, withdraw available funds, and request official court review for cases.

Court Employees review lawyer verification documents, approve or reject identity-change requests, create official cases for clients, review official case/hearing requests, schedule hearings, close cases, or postpone them.

Super Admins can manage Court Employee accounts and review platform counts.

---

## Main Roles

### Client

A client can:

- Create an account.
- Complete personal profile.
- View official cases assigned by Court Employees.
- Find verified lawyers.
- Send direct requests to lawyers.
- Create public legal requests.
- Upload case files.
- Chat with assigned lawyers.
- Pay or decline lawyer payment requests.
- Request changes to locked verified identity information.
- View hearing countdowns and case status.

### Lawyer

A lawyer can:

- Sign up with verification documents.
- Wait for Court Employee approval.
- Complete professional profile.
- Receive direct client requests.
- Submit proposals to public requests.
- View assigned cases.
- Chat with clients.
- Send payment requests.
- Edit pending payment requests.
- Receive paid amounts in lawyer balance.
- Withdraw part of available balance.
- Request Court Employee review to make a case official.
- View hearing countdowns.

### Court Employee

A Court Employee can:

- Review and approve/reject new lawyer accounts.
- Review lawyer legal ID and lawyer proof documents.
- Review identity-change requests.
- Create official cases for clients.
- Review official case/hearing requests.
- Schedule hearings with start and end time.
- Set next hearing dates.
- Postpone cases.
- Close cases.
- View dashboard counts for pending reviews and follow-ups.

### Super Admin

A Super Admin can:

- Access the internal admin dashboard.
- Create Court Employee accounts.
- View Court Employee details.
- Disable/remove Court Employee accounts safely.
- Review dashboard counts.
- Access lawyer approval, identity-change, and hearing-request pages.

---

## Core Features

### Authentication and Authorization

- ASP.NET Core Identity-based login and registration.
- Role-based access control.
- Public signup is limited to Client and Lawyer.
- Court Employee accounts are created internally by Super Admin.
- Super Admin seeded for development.
- Access-denied and error pages use JDSP styling.

### Lawyer Approval

- Lawyer signup requires verification documents.
- Required lawyer documents:
  - National/legal ID
  - Lawyer ID or proof of lawyer status
- New lawyers remain pending until approved.
- Rejected lawyers receive a reason.
- Approved lawyers can access the lawyer workspace.

### Case Management

- Court Employee creates official cases and assigns them to clients.
- Clients cannot create official cases directly.
- Clients can request a lawyer for an existing official case.
- If the lawyer accepts, the same case is assigned to that lawyer.
- The system avoids creating duplicate cases for existing official case requests.

### Client-Lawyer Requests

- Clients can send direct requests to lawyers.
- Clients can create public requests.
- Lawyers can accept direct requests or propose on public requests.
- Accepted direct/public requests appear in the lawyer’s assigned cases.
- Existing accepted requests are backfilled into assigned cases.

### Messaging

- Client and lawyer can chat when connected through accepted requests or cases.
- System messages are read-only.
- JDSP System messages use the official JDSP image.
- Unread message count is shown beside the Messages link.
- Chat auto-refreshes while the page is open.
- Payment messages update in place instead of duplicating.

### Identity Change Requests

Verified identity fields are locked by default:

- Full name
- Phone number
- National number

Users can request changes by submitting:

- New full name, phone number, or national number
- Legal ID attachment
- Optional reason

Court Employee/Admin can approve or reject the request.

If approved:

- User identity fields update automatically.
- User receives a JDSP System notification.

If rejected:

- Rejection reason is sent as a read-only JDSP System message.

### Payment Requests

Lawyers can send payment requests from assigned cases.

Payment requests include:

- Case name
- Amount
- Payment type:
  - One time / per case
  - Per month
  - Per hour
- Reference
- Notes

Clients can:

- Pay
- Decline with reason

When paid:

- The payment card turns green.
- The payment button disappears for that case.
- The lawyer’s balance is updated.
- The lawyer receives a notification.

When declined:

- The same payment card updates with decline reason.
- No duplicate payment message is created.

### Lawyer Balance and Withdrawals

Lawyer balance page shows:

- Available balance
- Total paid
- Withdrawn amount
- Payment history

Lawyer can withdraw a custom amount, not only full balance.

The withdrawal form includes:

- Card holder name
- Card number
- Expiry
- CVV
- Withdrawal amount

Card number auto-formats as:

```text
1234 - 5678 - 1234 - 5678
```

Expiry auto-formats as:

```text
02/22
```

### Hearing Schedules

Court Employee/Admin can schedule hearings with:

- Start date/time
- End date/time
- Type
- Location
- Notes

Countdown behavior:

- Before hearing start: green countdown to start time.
- During hearing: blue countdown to end time.
- After hearing ends: case status becomes waiting for next hearing date.
- Court Employee dashboard shows cases needing follow-up.
- Court Employee can:
  - Schedule next hearing
  - Postpone case
  - Close case

---

## Technology Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- Razor Views
- Bootstrap/custom CSS
- jQuery validation
- HTML/CSS/JavaScript
- English/Arabic localization
- RTL/LTR layout support

---

## Project Structure

The current project is an MVC-based structure.

Typical folders include:

```text
JDSP/
├── Controllers/
├── Data/
├── Entities/
├── Migrations/
├── Models/
├── ViewModels/
├── Views/
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   └── uploads/
├── App_Data/
├── Program.cs
└── appsettings.json
```

Important file areas:

```text
wwwroot/uploads/
```

Stores public profile images and uploaded public files.

```text
App_Data/
```

Stores protected verification files and legal documents that should not be directly public.

---

## Getting Started

### 1. Clone or extract the project

If using ZIP:

```powershell
Extract the ZIP file
Open the solution in Visual Studio
```

### 2. Configure the database connection

Open:

```text
appsettings.json
```

Update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=JDSP;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Adjust the server name depending on your SQL Server setup.

Examples:

```text
Server=.;
Server=localhost;
Server=.\SQLEXPRESS;
```

### 3. Restore packages

Visual Studio usually restores packages automatically.

You can also run:

```powershell
dotnet restore
```

### 4. Apply database migrations

In Visual Studio Package Manager Console:

```powershell
Update-Database
```

Or with .NET CLI:

```powershell
dotnet ef database update
```

### 5. Run the project

Use Visual Studio:

```text
Start Debugging
```

Or CLI:

```powershell
dotnet run
```

---

## Database Setup

The project uses EF Core migrations.

After every new ZIP/version that includes a migration, run:

```powershell
Update-Database
```

Do not delete migrations unless the team intentionally wants to reset the whole database.

Do not reset the database unless you are okay with losing test users, cases, payments, requests, and hearing data.

---

## Seeded Development Accounts

### Super Admin

For development/team testing:

```text
Email: superadmin@jdsp.local
Password: SuperAdmin@123
Role: Admin
```

Use this account to:

- Create Court Employee accounts
- Manage Court Employee users
- Review internal dashboards

Important:

This seeded account is for development only. Before production, change the password or remove the seeded account.

### Court Employee Accounts

Court Employee accounts should not be created through public signup.

Recommended flow:

1. Login as Super Admin.
2. Open Manage Employees.
3. Create a Court Employee.
4. Share the generated email/password with the team member.
5. The employee changes the temporary password on first login.

---

## Main Workflows

### Lawyer Signup and Approval

```text
Lawyer signs up
↓
Uploads National ID and Lawyer ID/proof
↓
Account becomes Pending
↓
Court Employee reviews documents
↓
Court Employee approves or rejects
↓
Approved lawyer completes professional profile
↓
Lawyer can use workspace
```

### Client Sends Request to Lawyer

```text
Client opens Find Lawyers
↓
Client selects lawyer
↓
Client sends request with subject, brief, and case file
↓
Lawyer sees request in Client Requests
↓
Lawyer accepts
↓
Case appears in Assigned Cases
↓
Client and lawyer can chat
```

### Client Requests Lawyer for Existing Official Case

```text
Court Employee creates official case for client
↓
Client opens case details
↓
Client clicks Find a lawyer
↓
Client sends request linked to that existing case
↓
Lawyer accepts
↓
Same official case is assigned to lawyer
↓
No duplicate case is created
```

### Payment Flow

```text
Lawyer opens assigned case
↓
Lawyer creates payment request
↓
Payment request appears in client DM
↓
Client pays or declines
↓
If paid:
    Payment becomes green
    Lawyer balance increases
    Lawyer receives notification
↓
If declined:
    Same payment card updates with decline reason
```

### Hearing Flow

```text
Lawyer requests court review/hearing
↓
Court Employee reviews request
↓
Court Employee approves and schedules hearing
↓
Client and lawyer see countdown
↓
When hearing ends:
    Case needs follow-up
↓
Court Employee schedules next hearing, postpones, or closes case
```

---

## File Uploads

### Public uploads

Profile images and general public uploads are stored under:

```text
wwwroot/uploads/
```

When moving to a new ZIP version, copy this folder from the old project to the new one:

```text
old-project/wwwroot/uploads/
```

to:

```text
new-project/wwwroot/uploads/
```

Otherwise, images may appear broken because the database stores the file path, but the physical file is missing.

### Protected uploads

Sensitive documents should be stored under:

```text
App_Data/
```

Examples:

- Lawyer legal verification documents
- Identity-change legal ID attachments

These files should be accessed through authorized controller actions, not directly from the browser.

---

## Localization

JDSP supports English and Arabic.

Important rules:

- Public Home page has a language toggle.
- After login, language is changed from Settings.
- The selected language is remembered.
- New/edited views must support English and Arabic.
- RTL/LTR direction must be respected.
- Future pages should not contain hard-coded English only.

Language toggle behavior:

```text
If UI is English → button shows AR
If UI is Arabic → button shows EN
```

---

## Payments and Lawyer Balance

Payment statuses include:

```text
Pending
Paid
Declined
```

Important behavior:

- Pending payments can be edited by the lawyer.
- Editing a payment updates the same DM payment card.
- Paying or declining updates the same DM card.
- Paid payment cards turn green.
- Declined payment cards show the reason.
- Successful payments are added to lawyer balance.
- Lawyers can withdraw part of available balance.

---

## Hearing Schedule Logic

The hearing countdown depends on current time:

### Before start time

```text
Countdown to start
Color: Green
```

### Between start and end time

```text
Countdown to end
Color: Blue
```

### After end time

```text
Countdown removed
Case status: Waiting for next hearing date
Court Employee dashboard count increases
```

Court Employee can then:

```text
Schedule next hearing
Postpone case
Close case
```

---

## Important Development Notes

### Do not delete migrations

If a database error happens, do not delete migrations immediately.

First check:

- Was the migration partially applied?
- Is the database already updated?
- Is this a cascade path issue?
- Is this a missing column issue?

### Uploaded images are not stored in the database

The database stores paths like:

```text
/uploads/profiles/example.png
```

But the real file exists physically in:

```text
wwwroot/uploads/profiles/example.png
```

Always copy `wwwroot/uploads` when moving to a new ZIP version.

### CSS consistency

All role workspaces should use the same visual system:

- Client workspace
- Lawyer workspace
- Court Employee workspace
- Super Admin workspace

Avoid default MVC styling in new views.

### Language consistency

Every new view must include English/Arabic support.

---

## Troubleshooting

### Broken profile images

Cause:

The database has the image path, but the image file does not exist in the new project folder.

Fix:

Copy:

```text
wwwroot/uploads/
```

from the old project to the new project.

### SQL Server multiple cascade paths

Example error:

```text
Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths.
```

Fix:

Set one relationship delete behavior to:

```text
Restrict
NoAction
```

Do not reset the database.

### Update-Database fails because object already exists

This may mean a previous migration partially ran.

Do not delete files immediately. Check the migration history table:

```sql
SELECT * FROM __EFMigrationsHistory;
```

### File upload says invalid mimetype

Cause:

Client-side validation may reject a valid file because of MIME/extension mismatch.

Fix:

Use extension validation and server-side validation together.

### Payment expiry/card validation error

Cause:

jQuery validation may fail if card/expiry formatting conflicts with numeric/date validation.

Fix:

Use safe text fields, formatting JavaScript, and matching server-side validation.

---

## Recommended Future Refactor

The project is now large enough to refactor into N-Tier Architecture.

Recommended future structure:

```text
JDSP.sln
│
├── JDSP.Web
│   ├── Controllers
│   ├── Views
│   ├── ViewModels
│   ├── wwwroot
│   └── Program.cs
│
├── JDSP.BLL
│   ├── Services
│   ├── Interfaces
│   ├── DTOs
│   └── Helpers
│
├── JDSP.DAL
│   ├── Data
│   ├── Repositories
│   ├── Migrations
│   └── Configurations
│
└── JDSP.Domain
    ├── Entities
    ├── Enums
    └── Constants
```

Dependency direction:

```text
JDSP.Web → JDSP.BLL → JDSP.DAL → JDSP.Domain
```

Start refactoring gradually:

1. Create projects.
2. Move entities/enums/constants to Domain.
3. Move DbContext/migrations to DAL.
4. Refactor Lawyer Directory first.
5. Refactor service requests.
6. Refactor cases.
7. Refactor payments/balance.
8. Refactor hearings.
9. Refactor messages.

Do not refactor all modules at once.

---

## Team Testing Checklist

Before submitting or presenting the project, test:

- Client signup
- Lawyer signup with documents
- Court Employee lawyer approval
- Lawyer professional profile completion
- Client sends request to lawyer
- Lawyer accepts request
- Assigned case appears
- Chat works
- Lawyer sends payment request
- Client pays
- Lawyer balance updates
- Lawyer withdraws partial amount
- Client declines payment with reason
- Court Employee creates official case
- Existing case request does not duplicate cases
- Lawyer requests official court review
- Court Employee schedules hearing
- Hearing countdown changes correctly
- Hearing follow-up works
- Identity change request approval/rejection
- Arabic/English switching
- RTL/LTR layout
- Error and access denied pages

---

## License

This project is developed for educational and graduation/project purposes.

Before public deployment, add a proper license file such as MIT, Apache-2.0, or a custom proprietary license depending on the team’s decision.
