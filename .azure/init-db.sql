-- Create database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BookRatingDB1')
BEGIN
    CREATE DATABASE BookRatingDB;
END
GO

USE BookRatingDB1;
GO
