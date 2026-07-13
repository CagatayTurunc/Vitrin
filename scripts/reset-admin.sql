-- Şifreyi "Admin1234!" olarak sıfırla (bcrypt hash, cost=11)
-- Hash: $2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.
UPDATE "Users" 
SET "PasswordHash" = '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.'
WHERE "Email" = 'admin123@gmail.com';

SELECT "Email", "Role", "PasswordHash" FROM "Users" WHERE "Email" = 'admin123@gmail.com';
