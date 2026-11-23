-- Check whether CollectionId is an identity column
SELECT
    COLUMNPROPERTY(
        OBJECT_ID('dbo.EMICollections'),
        'CollectionId',
        'IsIdentity'
    ) AS IsIdentity;

-- Check primary key and unique constraints
EXEC sp_helpconstraint 'dbo.EMICollections';

-- Check indexes
EXEC sp_helpindex 'dbo.EMICollections';

-- Check existing collection records
SELECT COUNT(*) AS TotalCollectionRecords
FROM dbo.EMICollections;


-- Check whether CollectionId is auto-generated
SELECT
    COLUMNPROPERTY(
        OBJECT_ID('dbo.EMICollections'),
        'CollectionId',
        'IsIdentity'
    ) AS IsIdentity;

-- Check existing collection records
SELECT
    COUNT(*) AS TotalCollectionRecords
FROM dbo.EMICollections;