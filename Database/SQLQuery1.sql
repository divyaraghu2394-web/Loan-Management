select * from Customers

select * from EMICollections

SELECT DB_NAME()


-- Check Customers table
EXEC sp_help 'dbo.Customers';

-- Check EMICollections table
EXEC sp_help 'dbo.EMICollections';

-- Check existing collection records
SELECT *
FROM dbo.EMICollections
ORDER BY CollectionId;


EXEC sp_help 'dbo.EMICollections';

SELECT *
FROM dbo.EMICollections
ORDER BY CollectionId;



USE LoanManagementDB;
GO

-- Check loan account number sequence
SELECT
    name,
    start_value,
    increment,
    current_value
FROM sys.sequences
WHERE name = 'Seq_LoanAccountNo';

-- Check Customers table indexes and constraints
EXEC sp_helpconstraint 'dbo.Customers';

EXEC sp_helpindex 'dbo.Customers';


USE LoanManagementDB;
GO

ALTER TABLE dbo.Customers
DROP CONSTRAINT CK_Customers_InstallmentCount;
GO

ALTER TABLE dbo.Customers
ADD CONSTRAINT CK_Customers_InstallmentCount
CHECK (InstallmentCount > 0);
GO

EXEC sp_helpconstraint 'dbo.Customers';

SELECT
    name,
    current_value,
    start_value,
    increment
FROM sys.sequences
WHERE name = 'Seq_LoanAccountNo';


USE LoanManagementDB;
GO

-- Check existing customer records
SELECT COUNT(*) AS TotalCustomers
FROM dbo.Customers;

-- Check loan account sequence
SELECT
    name,
    start_value,
    current_value,
    increment
FROM sys.sequences
WHERE name = 'Seq_LoanAccountNo';

-- Check active loans
SELECT COUNT(*) AS ActiveLoans
FROM dbo.Customers
WHERE IsLive = 1;



USE LoanManagementDB;
GO

BEGIN TRANSACTION;

-- Remove indexes on the old plain-text columns
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Customers_AadhaarNumber'
      AND object_id = OBJECT_ID('dbo.Customers')
)
    DROP INDEX IX_Customers_AadhaarNumber ON dbo.Customers;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Customers_PANNumber'
      AND object_id = OBJECT_ID('dbo.Customers')
)
    DROP INDEX IX_Customers_PANNumber ON dbo.Customers;

-- Rename columns to make their encrypted purpose clear
IF COL_LENGTH('dbo.Customers', 'AadhaarEncrypted') IS NULL
    EXEC sp_rename
        'dbo.Customers.AadhaarNumber',
        'AadhaarEncrypted',
        'COLUMN';

IF COL_LENGTH('dbo.Customers', 'PANEncrypted') IS NULL
    EXEC sp_rename
        'dbo.Customers.PANNumber',
        'PANEncrypted',
        'COLUMN';

-- Encrypted Base64 values need more storage than the original values
ALTER TABLE dbo.Customers
ALTER COLUMN AadhaarEncrypted VARCHAR(200) NOT NULL;

ALTER TABLE dbo.Customers
ALTER COLUMN PANEncrypted VARCHAR(200) NOT NULL;

-- Add deterministic HMAC lookup columns
IF COL_LENGTH('dbo.Customers', 'AadhaarHash') IS NULL
    ALTER TABLE dbo.Customers
    ADD AadhaarHash BINARY(32) NOT NULL;

IF COL_LENGTH('dbo.Customers', 'PANHash') IS NULL
    ALTER TABLE dbo.Customers
    ADD PANHash BINARY(32) NOT NULL;

-- Indexes support secure duplicate searches
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Customers_AadhaarHash'
      AND object_id = OBJECT_ID('dbo.Customers')
)
    CREATE INDEX IX_Customers_AadhaarHash
    ON dbo.Customers(AadhaarHash);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Customers_PANHash'
      AND object_id = OBJECT_ID('dbo.Customers')
)
    CREATE INDEX IX_Customers_PANHash
    ON dbo.Customers(PANHash);

COMMIT TRANSACTION;
GO



EXEC sp_help 'dbo.Customers';


USE LoanManagementDB;
GO

DROP INDEX IF EXISTS IX_Customers_AadhaarHash
ON dbo.Customers;

DROP INDEX IF EXISTS IX_Customers_PANHash
ON dbo.Customers;

ALTER TABLE dbo.Customers DROP COLUMN AadhaarHash;
ALTER TABLE dbo.Customers DROP COLUMN PANHash;

EXEC sp_rename
    'dbo.Customers.AadhaarEncrypted',
    'AadhaarNumber',
    'COLUMN';

EXEC sp_rename
    'dbo.Customers.PANEncrypted',
    'PANNumber',
    'COLUMN';

ALTER TABLE dbo.Customers
ALTER COLUMN AadhaarNumber VARCHAR(12) NOT NULL;

ALTER TABLE dbo.Customers
ALTER COLUMN PANNumber VARCHAR(10) NOT NULL;

CREATE INDEX IX_Customers_AadhaarNumber
ON dbo.Customers(AadhaarNumber);

CREATE INDEX IX_Customers_PANNumber
ON dbo.Customers(PANNumber);
GO


EXEC sp_help 'dbo.Customers';



USE LoanManagementDB;
GO

EXEC sp_help 'EMICollections';
GO

SELECT *
FROM EMICollections;
GO


EXEC sp_help 'EMICollections';
GO

EXEC sp_helpindex 'EMICollections';
GO

SELECT
    name,
    type_desc,
    is_unique,
    is_primary_key
FROM sys.indexes
WHERE object_id = OBJECT_ID('EMICollections');
GO


SELECT *
FROM EMICollections
ORDER BY CollectionId DESC;
GO

EXEC sp_help 'Customers';
GO

select * from customers