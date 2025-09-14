This is the test files directory for the AuditTrail system.

Files uploaded through the web interface will be stored here during testing.

The directory structure will be organized by date:
- YYYY/MM/DD/filename_guid.ext

This allows for easy organization and prevents filename conflicts.

Configuration:
- Storage Provider: Local (configured in appsettings.json)
- Base Path: C:\Work\oncooo\AuditTrail\git\TestFiles
- Max File Size: 100MB
- Allowed Extensions: .pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt, .jpg, .jpeg, .png, .gif, .zip, .rar

To switch to S3 storage later:
1. Update FileStorage:Provider to "S3" in appsettings.json
2. Configure AWS credentials and bucket settings
3. Uncomment S3 implementation in S3FileStorageService.cs
4. Add AWS SDK NuGet packages