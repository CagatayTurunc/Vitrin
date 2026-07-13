$env:PGPASSWORD="123456"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -d vitrin_auth -c "\d `"Users`""
