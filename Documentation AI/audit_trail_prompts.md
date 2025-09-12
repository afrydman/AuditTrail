
# Audit Trail System — Developer Prompts

This file contains a modular prompt breakdown for the development of a CFR 21 Part 11-compliant audit trail system. Each prompt can be assigned to a developer or AI agent for implementation.

---

## 1. 🔐 User & Role Management

Design and implement a user management system with **username/password** login and 14 fixed roles:

- Site Admin  
- Study Coordinator  
- Study Investigator  
- Unblinded Study Staff  
- Blinded Monitor  
- Unblinded Monitor  
- Sponsor Support  
- Auditor  
- Blinded Archivist  
- Unblinded Archivist  
- Binder Setup Blinded  
- Quality Control  
- System Support  
- System Team Setup  

Users must be able to log in, and their actions should be tied to their user ID and role.  
Add support for account lockout after multiple failed login attempts.  
Store user sessions and track login/logout events in an audit trail.

---

## 2. 📁 File Upload and Versioning

Implement a file upload system in .NET Razor MVC that:

- Accepts `.pdf`, `.doc`, and `.docx` files up to 50MB.
- Stores files in **cloud storage**.
- Organizes files by **path/context**, and uses the file name to manage **versioning**.
- If a file with the same name in the same path is uploaded, assign version `n+1` automatically.
- Supports simultaneous uploads of the same file: both should get incremented versions (e.g., v2, v3).
- Store file metadata (uploader, timestamp, path, version, etc.) in SQL Server.

---

## 3. 🧾 Audit Trail Logging (CFR 21 Part 11 Compliant)

Build an **immutable audit trail system** in SQL Server that logs:

- **All file events**: upload, view, delete, rename, version updates, and property changes.
- **All user events**: login, logout, failed login, and any file access.
- Each log entry must include:
  - Action type
  - Timestamp
  - User ID
  - Role
  - IP address
  - If applicable: **old and new values** (e.g., for rename or metadata updates)
- Audit logs must be stored immutably and retained for **1 year**.
- No tampering detection is required for now.
- Logs are stored and queried from the database (no export needed at this stage).

---

## 4. 🖥️ User Interface (Razor MVC)

Design Razor MVC views for:

- User dashboard: show uploaded files, versions, recent actions.
- Upload form: allow users to upload documents with path/context.
- File browser: view/search files by name, uploader, date, version, etc.
- File viewer: open/download a file.
- Audit viewer: show audit trail entries for a given file or user.

Add notifications when:

- A new version of a file is uploaded
- A file is deleted or modified

---

## 5. 🛡️ File Access Control

Implement access control so users can only:

- View or edit files according to their **role**.
- Not all roles can see all files — define a **role-to-permission map** (start with restrictive access and make it configurable later).

All file access (view/download) must be **logged in the audit trail** with timestamp, user, and IP.

---

## 6. 🗃️ Database Design (SQL Server)

Design the schema for:

- Users & Roles
- Files & Versions
- Audit Trail Entries
- File metadata (filename, path, uploader, upload time, etc.)

Focus on referential integrity and performance for read-heavy queries.

---

## 7. 🚫 Compliance Safeguards

Add safeguards to meet CFR 21 Part 11 electronic records rules (excluding digital signatures):

- Each record must be time-stamped and traceable to a user.
- Prevent unauthorized modification of audit logs.
- Maintain the original version of uploaded files.
- Ensure any changes to file metadata or versions are also audited.

---

## 8. ❓ Explore API Requirement

Analyze whether we will need a REST API layer in the future for:

- Integrating with external audit/reporting tools.
- Uploading or retrieving files programmatically.

If yes, suggest a clean way to structure the API with authentication and rate limiting.
