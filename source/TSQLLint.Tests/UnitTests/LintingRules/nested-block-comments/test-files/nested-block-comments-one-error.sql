SELECT *
FROM dbo.Foo
/* outer comment
   /* inner comment */
   more text
*/
WHERE Name = 'Bar'
GO
