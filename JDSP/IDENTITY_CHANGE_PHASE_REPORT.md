# JDSP Identity Change + Court Employee Styling Phase Report

## Base version
This update was applied on top of:

`JDSP-court-employee-lawyer-approval-phase(1).zip`

## User request covered
- Court Employee/Admin pages must use the same visual style as the rest of the platform.
- Lawyers/users should be able to request changes to locked verified identity information.
- Identity changes must not apply immediately.
- The request must be reviewed by a Court Employee/Admin.
- If approved, the information changes automatically.
- If rejected, the employee writes a reason.
- The user receives the rejection/approval as a read-only system message.
- Messages sidebar shows an unread count.
- Rejected/official messages appear under the same system sender.

## New database tables
A new migration was added:

`20260709212000_AddIdentityChangeRequestsAndSystemNotifications`

It creates:

### IdentityChangeRequests
Stores requests to change locked verified identity fields:
- Current full name
- Requested full name
- Current phone number
- Requested phone number
- Current national number
- Requested national number
- Legal ID attachment name/path
- Status: Pending / Approved / Rejected
- Rejection reason
- Reviewer
- Requested/reviewed dates

### SystemNotifications
Stores read-only system messages shown in the user Messages page:
- Recipient user
- Title
- Body
- Category
- Read/unread status
- Created date

## New files added
### Models
- `Models/IdentityChangeRequest.cs`
- `Models/SystemNotification.cs`

### Helpers
- `Helpers/IdentityFileHelper.cs`

### Controllers
- `Controllers/IdentityChangeRequestsController.cs`

### ViewModels
- `ViewModel/IdentityChangeRequests/IdentityChangeRequestViewModels.cs`
- `ViewModel/Messages/MessagesIndexViewModel.cs`

### Views
- `Views/IdentityChangeRequests/Index.cshtml`
- `Views/IdentityChangeRequests/Details.cshtml`
- `Views/IdentityChangeRequests/Reject.cshtml`
- `Views/Shared/_CourtLayout.cshtml`

## Modified files
### Database
- `Data/AppDbContext.cs`
- `Migrations/ApplicationDbContextModelSnapshot.cs`

### Profile identity change flow
- `Controllers/ProfileController.cs`
- `ViewModel/Profile/ClientProfileViewModel.cs`
- `Views/Profile/Index.cshtml`

### Messages system notifications
- `Controllers/MessagesController.cs`
- `Views/Messages/Index.cshtml`
- `Views/Shared/_ClientLayout.cshtml`
- `Views/Shared/_LawyerLayout.cshtml`
- `wwwroot/css/client-layout.css`

### Court/Admin styling
- `Views/Shared/_CourtLayout.cshtml`
- `Views/Dashboard/AdminDashboard.cshtml`
- `Views/Dashboard/CourtEmployeeDashboard.cshtml`
- `Views/Admin/CourtEmployees.cshtml`
- `Views/Admin/CreateCourtEmployee.cshtml`
- `Views/LawyerApprovals/Index.cshtml`
- `Views/LawyerApprovals/Details.cshtml`
- `Views/LawyerApprovals/Reject.cshtml`
- `Views/Cases/Create.cshtml`

### Court Employee dashboard data
- `Controllers/DashboardController.cs`
- `ViewModel/CourtEmployee/CourtEmployeeDashboardViewModel.cs`

## User flow: Identity change request
1. User opens Edit Profile.
2. In Verified Identity, user can enter requested:
   - Full name
   - Phone number
   - National number
3. User must attach legal ID.
4. The system creates a Pending identity change request.
5. User cannot send another identity request while one is pending.
6. Court Employee/Admin reviews it from Identity changes.
7. If approved:
   - User FirstName / MiddleName / LastName are updated.
   - PhoneNumber is updated.
   - NationalNumber is updated.
   - User receives a read-only system message.
8. If rejected:
   - No data is changed.
   - Rejection reason is saved.
   - User receives a read-only system message with the reason.

## Messages behavior
- Messages page now has a system sender: `JDSP System` / `رسائل النظام`.
- System messages are read-only.
- Users cannot reply to system messages.
- All official notifications appear under the same system sender.
- The sidebar Messages link shows an unread badge count.
- Opening Messages marks unread system notifications as read.

## Court Employee/Admin UI changes
- Added `_CourtLayout.cshtml` to match the platform style.
- Admin and Court Employee pages now use the same dark workspace style as client/lawyer pages.
- Added sidebar links for:
  - Dashboard
  - Lawyer approvals
  - Identity changes
  - Court employees for Admin
  - Create case for Court Employee

## Required command after extracting
Run:

```powershell
Update-Database
```

Do not delete migrations and do not reset the database.

## Notes
- Uploaded identity documents are stored privately in:

`App_Data/identity-change-requests`

They are not public files under `wwwroot`.

- Because the environment used for editing does not have the .NET SDK installed, the project structure and files were checked, but `dotnet build` could not be executed here.
