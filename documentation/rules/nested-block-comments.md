# Avoid Nested Block Comments

## Rule Details

Nested block comments are valid T-SQL, but some SQL tooling does not support them and can fail to parse a script.

Examples of **incorrect** code for this rule:

```tsql
SELECT *
FROM dbo.Foo
/* outer comment
   /* inner comment */
   more text
*/
WHERE Name = 'Bar'
```

Examples of **correct** code for this rule:

```tsql
SELECT *
FROM dbo.Foo
/* outer comment
-- inner comment
*/
WHERE Name = 'Bar'
```
