USE LoanManagementDB;
GO

-- Add installment number
IF COL_LENGTH('dbo.EMICollections', 'InstallmentNumber') IS NULL
BEGIN
    ALTER TABLE dbo.EMICollections
    ADD InstallmentNumber INT NOT NULL;
END
GO

-- Ensure installment number is valid
IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_EMICollections_InstallmentNumber'
)
BEGIN
    ALTER TABLE dbo.EMICollections
    ADD CONSTRAINT CK_EMICollections_InstallmentNumber
    CHECK (InstallmentNumber > 0);
END
GO

-- Prevent duplicate installment collection
IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_EMICollections_Loan_Installment'
      AND object_id = OBJECT_ID('dbo.EMICollections')
)
BEGIN
    CREATE UNIQUE INDEX UX_EMICollections_Loan_Installment
    ON dbo.EMICollections (LoanAccountNo, InstallmentNumber);
END
GO