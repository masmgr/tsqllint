# Disallows host-local date/time functions

## Rule Details

Local date/time functions depend on the OS time zone settings. Prefer UTC functions and convert in the client or with `AT TIME ZONE` as needed.

Examples of **incorrect** code for this rule:

```tsql
SELECT GETDATE();
SELECT SYSDATETIMEOFFSET();
```

Examples of **correct** code for this rule:

```tsql
SELECT SYSUTCDATETIME();
SELECT GETUTCDATE();
```
