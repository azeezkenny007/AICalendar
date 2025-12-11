-- =============================================
-- Script: Seed Transaction Data from JSON File
-- Description: Clears existing transaction data and seeds from JSON
-- Created: 2025-12-10
-- =============================================

USE AICalendarDb;
GO

-- Start transaction to ensure data integrity
BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting transaction data seeding...';

    -- =============================================
    -- Step 1: Delete all existing transactions
    -- =============================================
    PRINT 'Deleting existing transactions...';
    DELETE FROM Transactions;
    PRINT 'Existing transactions deleted successfully.';

    -- =============================================
    -- Step 2: Insert new transaction data
    -- =============================================
    PRINT 'Inserting new transaction data...';

    INSERT INTO Transactions (Id, UserId, Amount, Description, Type, TransactionDate, CreatedAt, IsDiscarded, IsKept, MerchantId, ReceiverId)
    VALUES
    -- User 1 Transactions (11111111-1111-1111-1111-111111111111)
    ('a8040dee-da7e-475d-b499-9dd735460d44', '11111111-1111-1111-1111-111111111111', 1471.53, 'MTN Airtime', 'AirtimeTopup', '2025-06-03 00:00:00', '2025-06-03 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('39075790-0422-4fe9-8e9f-e12ea1aa19d6', '11111111-1111-1111-1111-111111111111', 11950.71, 'Cowrywise Savings', 'SavingsContribution', '2025-06-05 00:00:00', '2025-06-05 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('b02264b8-a813-4e09-92c3-a5e275d27a8d', '11111111-1111-1111-1111-111111111111', 1295.07, 'MTN Airtime', 'AirtimeTopup', '2025-06-10 00:00:00', '2025-06-10 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('7b13f358-c2a7-40bc-a2de-e547f7697c55', '11111111-1111-1111-1111-111111111111', 5098.05, 'DSTV Subscription', 'BillPayment', '2025-06-15 00:00:00', '2025-06-15 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('125a63a6-0776-4c42-bf6e-b84bab956420', '11111111-1111-1111-1111-111111111111', 1316.44, 'MTN Airtime', 'AirtimeTopup', '2025-06-17 00:00:00', '2025-06-17 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('7412aff6-cd96-4d97-a187-dda1f946b8c5', '11111111-1111-1111-1111-111111111111', 1357.08, 'MTN Airtime', 'AirtimeTopup', '2025-06-24 00:00:00', '2025-06-24 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('98d3f27b-fab9-45ae-ae64-d67cfc020f5a', '11111111-1111-1111-1111-111111111111', 8776.9, 'PHCN Electricity Bill', 'BillPayment', '2025-06-25 00:00:00', '2025-06-25 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('e7f7de3b-506b-4829-9fec-a50df246ecd6', '11111111-1111-1111-1111-111111111111', 1187.0, 'MTN Airtime', 'AirtimeTopup', '2025-07-01 00:00:00', '2025-07-01 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('cb2231ac-1f90-4bad-b9ec-4cd3d1293fcb', '11111111-1111-1111-1111-111111111111', 11737.64, 'Cowrywise Savings', 'SavingsContribution', '2025-07-05 00:00:00', '2025-07-05 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('604744b5-fb9f-4448-a804-6463863d01a2', '11111111-1111-1111-1111-111111111111', 1439.79, 'MTN Airtime', 'AirtimeTopup', '2025-07-08 00:00:00', '2025-07-08 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('4bddeef1-b5cf-44d0-baba-28b3302f244a', '11111111-1111-1111-1111-111111111111', 5454.8, 'DSTV Subscription', 'BillPayment', '2025-07-15 00:00:00', '2025-07-15 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('21c23369-52d3-4ee4-adea-51c2bbc7fd3e', '11111111-1111-1111-1111-111111111111', 1496.73, 'MTN Airtime', 'AirtimeTopup', '2025-07-15 00:00:00', '2025-07-15 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('145e13de-3c85-44cd-9637-5ab6af5ee30d', '11111111-1111-1111-1111-111111111111', 1402.94, 'MTN Airtime', 'AirtimeTopup', '2025-07-22 00:00:00', '2025-07-22 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('09bce159-0bf1-43b4-8d8e-945a85d81cb0', '11111111-1111-1111-1111-111111111111', 9667.37, 'PHCN Electricity Bill', 'BillPayment', '2025-07-25 00:00:00', '2025-07-25 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('44ffa52b-ee51-4aab-9eab-c2e7969c509a', '11111111-1111-1111-1111-111111111111', 1496.64, 'MTN Airtime', 'AirtimeTopup', '2025-07-29 00:00:00', '2025-07-29 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d978648f-0d07-443f-b922-97cc486090d7', '11111111-1111-1111-1111-111111111111', 22482.28, 'Online Shopping', 'OTHER', '2025-07-31 00:00:00', '2025-07-31 00:00:00', 0, 0, 'Jumia', 'Jumia'),
    ('38748857-5956-40f1-9c65-898d22d57614', '11111111-1111-1111-1111-111111111111', 1073.65, 'MTN Airtime', 'AirtimeTopup', '2025-08-05 00:00:00', '2025-08-05 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('e61aca35-50fa-44b6-903f-6e7a129d2961', '11111111-1111-1111-1111-111111111111', 11289.7, 'Cowrywise Savings', 'SavingsContribution', '2025-08-05 00:00:00', '2025-08-05 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('50678d35-4657-4d34-a086-d878ee8a951b', '11111111-1111-1111-1111-111111111111', 1332.11, 'MTN Airtime', 'AirtimeTopup', '2025-08-12 00:00:00', '2025-08-12 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('dca1ac87-0401-4d29-b0de-85c971146282', '11111111-1111-1111-1111-111111111111', 5354.41, 'DSTV Subscription', 'BillPayment', '2025-08-15 00:00:00', '2025-08-15 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('f957d9bf-828c-4078-aa9d-0d87e86447bf', '11111111-1111-1111-1111-111111111111', 1152.49, 'MTN Airtime', 'AirtimeTopup', '2025-08-19 00:00:00', '2025-08-19 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('86f2e18b-92c1-48e0-87ac-929938dffd86', '11111111-1111-1111-1111-111111111111', 11767.79, 'PHCN Electricity Bill', 'BillPayment', '2025-08-25 00:00:00', '2025-08-25 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('ac5e16a8-93fa-4f2c-932b-1c885a9c8409', '11111111-1111-1111-1111-111111111111', 1276.63, 'MTN Airtime', 'AirtimeTopup', '2025-08-26 00:00:00', '2025-08-26 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('75e5e479-768d-4cdf-900e-d3e1bb58c452', '11111111-1111-1111-1111-111111111111', 1200.53, 'MTN Airtime', 'AirtimeTopup', '2025-09-02 00:00:00', '2025-09-02 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('7cda5a47-7e65-4b60-b666-660b96b4c385', '11111111-1111-1111-1111-111111111111', 11087.69, 'Cowrywise Savings', 'SavingsContribution', '2025-09-05 00:00:00', '2025-09-05 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('4c6c14a9-9fbe-4f85-bcf3-cb0f6692c6a2', '11111111-1111-1111-1111-111111111111', 1039.28, 'MTN Airtime', 'AirtimeTopup', '2025-09-09 00:00:00', '2025-09-09 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('090837ac-37f1-4684-be22-a7114bda5e88', '11111111-1111-1111-1111-111111111111', 5477.71, 'DSTV Subscription', 'BillPayment', '2025-09-15 00:00:00', '2025-09-15 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('fcf4cad9-5c09-4ad3-ad37-6daa9d8664e9', '11111111-1111-1111-1111-111111111111', 1394.18, 'MTN Airtime', 'AirtimeTopup', '2025-09-16 00:00:00', '2025-09-16 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('451f80ee-5e5e-4bcd-96ce-07c85d599560', '11111111-1111-1111-1111-111111111111', 1053.41, 'MTN Airtime', 'AirtimeTopup', '2025-09-23 00:00:00', '2025-09-23 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('3d75a428-4242-4c8f-8551-e89a3822781c', '11111111-1111-1111-1111-111111111111', 9543.95, 'PHCN Electricity Bill', 'BillPayment', '2025-09-25 00:00:00', '2025-09-25 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('73ff75b7-3038-4439-bce8-dacb44f725f2', '11111111-1111-1111-1111-111111111111', 1248.44, 'MTN Airtime', 'AirtimeTopup', '2025-09-30 00:00:00', '2025-09-30 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('c4f95848-c9ec-47e7-b852-5c2eae1f5cbb', '11111111-1111-1111-1111-111111111111', 10024.22, 'Cowrywise Savings', 'SavingsContribution', '2025-10-05 00:00:00', '2025-10-05 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('34ae8a9c-a508-4673-bc42-1506b2353f8a', '11111111-1111-1111-1111-111111111111', 2368.94, 'Uber Ride', 'OTHER', '2025-10-05 00:00:00', '2025-10-05 00:00:00', 0, 0, 'Uber', 'Uber'),
    ('63a303e8-c589-405d-a05a-5cb03b0df965', '11111111-1111-1111-1111-111111111111', 1162.27, 'MTN Airtime', 'AirtimeTopup', '2025-10-07 00:00:00', '2025-10-07 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('53a70ede-6e15-489c-ba5d-d996b9e3e213', '11111111-1111-1111-1111-111111111111', 1175.53, 'MTN Airtime', 'AirtimeTopup', '2025-10-14 00:00:00', '2025-10-14 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d66cf714-8d1e-4d26-8bf3-c3589de13952', '11111111-1111-1111-1111-111111111111', 5172.13, 'DSTV Subscription', 'BillPayment', '2025-10-15 00:00:00', '2025-10-15 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('92a64f97-dc2b-410d-ad6b-b371d5915980', '11111111-1111-1111-1111-111111111111', 1176.15, 'MTN Airtime', 'AirtimeTopup', '2025-10-21 00:00:00', '2025-10-21 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('0f8be21d-24a0-4859-949f-6252814bc39a', '11111111-1111-1111-1111-111111111111', 9341.01, 'PHCN Electricity Bill', 'BillPayment', '2025-10-25 00:00:00', '2025-10-25 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('6d20e941-868f-450c-b462-2d05b9f574e8', '11111111-1111-1111-1111-111111111111', 1180.55, 'MTN Airtime', 'AirtimeTopup', '2025-10-28 00:00:00', '2025-10-28 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('0a54a861-f7b1-44a2-98b0-2c6890a95f4d', '11111111-1111-1111-1111-111111111111', 10158.58, 'Online Shopping', 'OTHER', '2025-10-28 00:00:00', '2025-10-28 00:00:00', 0, 0, 'Jumia', 'Jumia'),
    ('e9b8a4c7-9f1a-4d3e-b5c2-8a7f3e1d9f4a', '11111111-1111-1111-1111-111111111111', 1285.75, 'MTN Airtime', 'AirtimeTopup', '2025-11-02 00:00:00', '2025-11-02 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('f2c8b5d9-6a3e-4f7c-8e1b-3c9a7e8f4d2a', '11111111-1111-1111-1111-111111111111', 10895.45, 'Cowrywise Savings', 'SavingsContribution', '2025-11-05 00:00:00', '2025-11-05 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d', '11111111-1111-1111-1111-111111111111', 1245.89, 'MTN Airtime', 'AirtimeTopup', '2025-11-09 00:00:00', '2025-11-09 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e', '11111111-1111-1111-1111-111111111111', 5321.67, 'DSTV Subscription', 'BillPayment', '2025-11-15 00:00:00', '2025-11-15 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f', '11111111-1111-1111-1111-111111111111', 1198.34, 'MTN Airtime', 'AirtimeTopup', '2025-11-18 00:00:00', '2025-11-18 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a', '11111111-1111-1111-1111-111111111111', 10235.89, 'PHCN Electricity Bill', 'BillPayment', '2025-11-25 00:00:00', '2025-11-25 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b', '11111111-1111-1111-1111-111111111111', 1278.45, 'MTN Airtime', 'AirtimeTopup', '2025-11-28 00:00:00', '2025-11-28 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c', '11111111-1111-1111-1111-111111111111', 2456.78, 'Uber Ride', 'OTHER', '2025-11-30 00:00:00', '2025-11-30 00:00:00', 0, 0, 'Uber', 'Uber'),
    ('a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d', '11111111-1111-1111-1111-111111111111', 1356.23, 'MTN Airtime', 'AirtimeTopup', '2025-06-08 00:00:00', '2025-06-08 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e', '11111111-1111-1111-1111-111111111111', 12145.67, 'Cowrywise Savings', 'SavingsContribution', '2025-06-20 00:00:00', '2025-06-20 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('c9d0e1f2-a3b4-4c5d-6e7f-8a9b0c1d2e3f', '11111111-1111-1111-1111-111111111111', 1412.89, 'MTN Airtime', 'AirtimeTopup', '2025-07-03 00:00:00', '2025-07-03 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d0e1f2a3-b4c5-4d6e-7f8a-9b0c1d2e3f4a', '11111111-1111-1111-1111-111111111111', 5213.45, 'DSTV Subscription', 'BillPayment', '2025-07-18 00:00:00', '2025-07-18 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('e1f2a3b4-c5d6-4e7f-8a9b-0c1d2e3f4a5b', '11111111-1111-1111-1111-111111111111', 1156.78, 'MTN Airtime', 'AirtimeTopup', '2025-08-03 00:00:00', '2025-08-03 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('f2a3b4c5-d6e7-4f8a-9b0c-1d2e3f4a5b6c', '11111111-1111-1111-1111-111111111111', 11456.89, 'Cowrywise Savings', 'SavingsContribution', '2025-08-08 00:00:00', '2025-08-08 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('a3b4c5d6-e7f8-4a9b-0c1d-2e3f4a5b6c7d', '11111111-1111-1111-1111-111111111111', 1234.56, 'MTN Airtime', 'AirtimeTopup', '2025-09-12 00:00:00', '2025-09-12 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('b4c5d6e7-f8a9-4b0c-1d2e-3f4a5b6c7d8e', '11111111-1111-1111-1111-111111111111', 5432.1, 'DSTV Subscription', 'BillPayment', '2025-09-28 00:00:00', '2025-09-28 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('c5d6e7f8-a9b0-4c1d-2e3f-4a5b6c7d8e9f', '11111111-1111-1111-1111-111111111111', 1189.45, 'MTN Airtime', 'AirtimeTopup', '2025-10-11 00:00:00', '2025-10-11 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d6e7f8a9-b0c1-4d2e-3f4a-5b6c7d8e9f0a', '11111111-1111-1111-1111-111111111111', 9876.54, 'PHCN Electricity Bill', 'BillPayment', '2025-10-18 00:00:00', '2025-10-18 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('e7f8a9b0-c1d2-4e3f-4a5b-6c7d8e9f0a1b', '11111111-1111-1111-1111-111111111111', 21543.21, 'Online Shopping', 'OTHER', '2025-11-08 00:00:00', '2025-11-08 00:00:00', 0, 0, 'Jumia', 'Jumia'),
    ('f8a9b0c1-d2e3-4f4a-5b6c-7d8e9f0a1b2c', '11111111-1111-1111-1111-111111111111', 1321.67, 'MTN Airtime', 'AirtimeTopup', '2025-11-14 00:00:00', '2025-11-14 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('a9b0c1d2-e3f4-4a5b-6c7d-8e9f0a1b2c3d', '11111111-1111-1111-1111-111111111111', 10987.65, 'Cowrywise Savings', 'SavingsContribution', '2025-11-20 00:00:00', '2025-11-20 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('b0c1d2e3-f4a5-4b6c-7d8e-9f0a1b2c3d4e', '11111111-1111-1111-1111-111111111111', 1256.89, 'MTN Airtime', 'AirtimeTopup', '2025-06-13 00:00:00', '2025-06-13 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f', '11111111-1111-1111-1111-111111111111', 5123.45, 'DSTV Subscription', 'BillPayment', '2025-06-28 00:00:00', '2025-06-28 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('d2e3f4a5-b6c7-4d8e-9f0a-1b2c3d4e5f6a', '11111111-1111-1111-1111-111111111111', 1198.76, 'MTN Airtime', 'AirtimeTopup', '2025-07-11 00:00:00', '2025-07-11 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('e3f4a5b6-c7d8-4e9f-0a1b-2c3d4e5f6a7b', '11111111-1111-1111-1111-111111111111', 9234.56, 'PHCN Electricity Bill', 'BillPayment', '2025-07-27 00:00:00', '2025-07-27 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('f4a5b6c7-d8e9-4f0a-1b2c-3d4e5f6a7b8c', '11111111-1111-1111-1111-111111111111', 1211.23, 'MTN Airtime', 'AirtimeTopup', '2025-08-07 00:00:00', '2025-08-07 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('a5b6c7d8-e9f0-4a1b-2c3d-4e5f6a7b8c9d', '11111111-1111-1111-1111-111111111111', 11345.67, 'Cowrywise Savings', 'SavingsContribution', '2025-08-22 00:00:00', '2025-08-22 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('b6c7d8e9-f0a1-4b2c-3d4e-5f6a7b8c9d0e', '11111111-1111-1111-1111-111111111111', 1267.89, 'MTN Airtime', 'AirtimeTopup', '2025-09-07 00:00:00', '2025-09-07 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('c7d8e9f0-a1b2-4c3d-4e5f-6a7b8c9d0e1f', '11111111-1111-1111-1111-111111111111', 5345.67, 'DSTV Subscription', 'BillPayment', '2025-09-22 00:00:00', '2025-09-22 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('d8e9f0a1-b2c3-4d4e-5f6a-7b8c9d0e1f2a', '11111111-1111-1111-1111-111111111111', 1187.65, 'MTN Airtime', 'AirtimeTopup', '2025-10-03 00:00:00', '2025-10-03 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('e9f0a1b2-c3d4-4e5f-6a7b-8c9d0e1f2a3b', '11111111-1111-1111-1111-111111111111', 9567.89, 'PHCN Electricity Bill', 'BillPayment', '2025-10-20 00:00:00', '2025-10-20 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('f0a1b2c3-d4e5-4f6a-7b8c-9d0e1f2a3b4c', '11111111-1111-1111-1111-111111111111', 2345.67, 'Uber Ride', 'OTHER', '2025-11-05 00:00:00', '2025-11-05 00:00:00', 0, 0, 'Uber', 'Uber'),
    ('a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5e', '11111111-1111-1111-1111-111111111111', 1298.76, 'MTN Airtime', 'AirtimeTopup', '2025-11-12 00:00:00', '2025-11-12 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6f', '11111111-1111-1111-1111-111111111111', 10765.43, 'Cowrywise Savings', 'SavingsContribution', '2025-11-25 00:00:00', '2025-11-25 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7a', '11111111-1111-1111-1111-111111111111', 1234.56, 'MTN Airtime', 'AirtimeTopup', '2025-06-06 00:00:00', '2025-06-06 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8b', '11111111-1111-1111-1111-111111111111', 11876.54, 'Cowrywise Savings', 'SavingsContribution', '2025-06-22 00:00:00', '2025-06-22 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9c', '11111111-1111-1111-1111-111111111111', 1345.67, 'MTN Airtime', 'AirtimeTopup', '2025-07-05 00:00:00', '2025-07-05 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0d', '11111111-1111-1111-1111-111111111111', 5234.56, 'DSTV Subscription', 'BillPayment', '2025-07-20 00:00:00', '2025-07-20 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1e', '11111111-1111-1111-1111-111111111111', 1198.9, 'MTN Airtime', 'AirtimeTopup', '2025-08-10 00:00:00', '2025-08-10 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2f', '11111111-1111-1111-1111-111111111111', 11234.56, 'Cowrywise Savings', 'SavingsContribution', '2025-08-18 00:00:00', '2025-08-18 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('c9d0e1f2-a3b4-4c5d-6e7f-8a9b0c1d2e3a', '11111111-1111-1111-1111-111111111111', 1256.78, 'MTN Airtime', 'AirtimeTopup', '2025-09-14 00:00:00', '2025-09-14 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d0e1f2a3-b4c5-4d6e-7f8a-9b0c1d2e3f4b', '11111111-1111-1111-1111-111111111111', 5432.1, 'DSTV Subscription', 'BillPayment', '2025-09-29 00:00:00', '2025-09-29 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('e1f2a3b4-c5d6-4e7f-8a9b-0c1d2e3f4a5d', '11111111-1111-1111-1111-111111111111', 1176.54, 'MTN Airtime', 'AirtimeTopup', '2025-10-09 00:00:00', '2025-10-09 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('f2a3b4c5-d6e7-4f8a-9b0c-1d2e3f4a5b6d', '11111111-1111-1111-1111-111111111111', 9678.9, 'PHCN Electricity Bill', 'BillPayment', '2025-10-30 00:00:00', '2025-10-30 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('a3b4c5d6-e7f8-4a9b-0c1d-2e3f4a5b6c7e', '11111111-1111-1111-1111-111111111111', 22567.89, 'Online Shopping', 'OTHER', '2025-11-03 00:00:00', '2025-11-03 00:00:00', 0, 0, 'Jumia', 'Jumia'),
    ('b4c5d6e7-f8a9-4b0c-1d2e-3f4a5b6c7d8f', '11111111-1111-1111-1111-111111111111', 1289.01, 'MTN Airtime', 'AirtimeTopup', '2025-11-16 00:00:00', '2025-11-16 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('c5d6e7f8-a9b0-4c1d-2e3f-4a5b6c7d8e9a', '11111111-1111-1111-1111-111111111111', 10876.54, 'Cowrywise Savings', 'SavingsContribution', '2025-11-27 00:00:00', '2025-11-27 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('d6e7f8a9-b0c1-4d2e-3f4a-5b6c7d8e9f0b', '11111111-1111-1111-1111-111111111111', 1321.45, 'MTN Airtime', 'AirtimeTopup', '2025-06-11 00:00:00', '2025-06-11 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('e7f8a9b0-c1d2-4e3f-4a5b-6c7d8e9f0a1c', '11111111-1111-1111-1111-111111111111', 11987.65, 'Cowrywise Savings', 'SavingsContribution', '2025-06-27 00:00:00', '2025-06-27 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('f8a9b0c1-d2e3-4f4a-5b6c-7d8e9f0a1b2d', '11111111-1111-1111-1111-111111111111', 1398.76, 'MTN Airtime', 'AirtimeTopup', '2025-07-13 00:00:00', '2025-07-13 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('a9b0c1d2-e3f4-4a5b-6c7d-8e9f0a1b2c3e', '11111111-1111-1111-1111-111111111111', 5123.45, 'DSTV Subscription', 'BillPayment', '2025-07-25 00:00:00', '2025-07-25 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('b0c1d2e3-f4a5-4b6c-7d8e-9f0a1b2c3d4f', '11111111-1111-1111-1111-111111111111', 1211.23, 'MTN Airtime', 'AirtimeTopup', '2025-08-14 00:00:00', '2025-08-14 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5a', '11111111-1111-1111-1111-111111111111', 11345.67, 'Cowrywise Savings', 'SavingsContribution', '2025-08-29 00:00:00', '2025-08-29 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),
    ('d2e3f4a5-b6c7-4d8e-9f0a-1b2c3d4e5f6b', '11111111-1111-1111-1111-111111111111', 1267.89, 'MTN Airtime', 'AirtimeTopup', '2025-09-04 00:00:00', '2025-09-04 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('e3f4a5b6-c7d8-4e9f-0a1b-2c3d4e5f6a7c', '11111111-1111-1111-1111-111111111111', 5345.67, 'DSTV Subscription', 'BillPayment', '2025-09-19 00:00:00', '2025-09-19 00:00:00', 0, 0, 'DSTV', 'DSTV'),
    ('f4a5b6c7-d8e9-4f0a-1b2c-3d4e5f6a7b8d', '11111111-1111-1111-1111-111111111111', 1187.65, 'MTN Airtime', 'AirtimeTopup', '2025-10-02 00:00:00', '2025-10-02 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('a5b6c7d8-e9f0-4a1b-2c3d-4e5f6a7b8c9e', '11111111-1111-1111-1111-111111111111', 9567.89, 'PHCN Electricity Bill', 'BillPayment', '2025-10-17 00:00:00', '2025-10-17 00:00:00', 0, 0, 'PHCN', 'PHCN'),
    ('b6c7d8e9-f0a1-4b2c-3d4e-5f6a7b8c9d0f', '11111111-1111-1111-1111-111111111111', 2345.67, 'Uber Ride', 'OTHER', '2025-11-07 00:00:00', '2025-11-07 00:00:00', 0, 0, 'Uber', 'Uber'),
    ('c7d8e9f0-a1b2-4c3d-4e5f-6a7b8c9d0e1a', '11111111-1111-1111-1111-111111111111', 1298.76, 'MTN Airtime', 'AirtimeTopup', '2025-11-15 00:00:00', '2025-11-15 00:00:00', 0, 0, 'MTN Nigeria', 'MTN Nigeria'),
    ('d8e9f0a1-b2c3-4d4e-5f6a-7b8c9d0e1f2b', '11111111-1111-1111-1111-111111111111', 10765.43, 'Cowrywise Savings', 'SavingsContribution', '2025-11-29 00:00:00', '2025-11-29 00:00:00', 0, 0, 'Cowrywise', 'Cowrywise'),

    -- User 2 Transactions (22222222-2222-2222-2222-222222222222)
    ('16f7610a-dd57-4b3d-b4d6-4e5faef7c705', '22222222-2222-2222-2222-222222222222', 25956.11, 'AXA Mansard Insurance', 'InsurancePayment', '2025-06-01 00:00:00', '2025-06-01 00:00:00', 0, 0, 'AXA Mansard', 'AXA Mansard'),
    ('851b7d7e-e2ed-496a-9852-ad2fea09b509', '22222222-2222-2222-2222-222222222222', 725.15, 'Glo Airtime', 'AirtimeTopup', '2025-06-07 00:00:00', '2025-06-07 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('cd3d3e3e-c35e-4fdb-b0a4-45922924b87b', '22222222-2222-2222-2222-222222222222', 2900.0, 'Netflix Subscription', 'DirectDebit', '2025-06-10 00:00:00', '2025-06-10 00:00:00', 0, 0, 'Netflix Nigeria', 'Netflix Nigeria'),
    ('8cf4a85b-cbaa-468f-8856-d2e468e0e3e6', '22222222-2222-2222-2222-222222222222', 32860.17, 'Transfer to Friend', 'TransferLocal', '2025-06-12 00:00:00', '2025-06-12 00:00:00', 0, 0, 'NULL', 'NULL'),
    ('b3f059f2-7c25-4eaa-b695-14ccd82ee073', '22222222-2222-2222-2222-222222222222', 821.51, 'Glo Airtime', 'AirtimeTopup', '2025-06-14 00:00:00', '2025-06-14 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('d83e03c4-e16c-43ac-aebb-061876b9c8e7', '22222222-2222-2222-2222-222222222222', 825.49, 'Glo Airtime', 'AirtimeTopup', '2025-06-21 00:00:00', '2025-06-21 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('023affa1-cb14-4034-af11-7e231da4bb14', '22222222-2222-2222-2222-222222222222', 922.74, 'Glo Airtime', 'AirtimeTopup', '2025-06-28 00:00:00', '2025-06-28 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('38c95e66-20a6-4d85-8745-4445966a6a21', '22222222-2222-2222-2222-222222222222', 15338.37, 'FairMoney Loan Repayment', 'LoanRepayment', '2025-06-28 00:00:00', '2025-06-28 00:00:00', 0, 0, 'FairMoney', 'FairMoney'),
    ('8dbc5c7b-7f3e-4f87-aaac-4193e817d312', '22222222-2222-2222-2222-222222222222', 25296.53, 'AXA Mansard Insurance', 'InsurancePayment', '2025-07-01 00:00:00', '2025-07-01 00:00:00', 0, 0, 'AXA Mansard', 'AXA Mansard'),
    ('14d4112c-ca76-443d-aca6-995cde17cf46', '22222222-2222-2222-2222-222222222222', 966.66, 'Glo Airtime', 'AirtimeTopup', '2025-07-05 00:00:00', '2025-07-05 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('a26414fe-87b5-459d-8cb7-0ffc692c878e', '22222222-2222-2222-2222-222222222222', 2900.0, 'Netflix Subscription', 'DirectDebit', '2025-07-10 00:00:00', '2025-07-10 00:00:00', 0, 0, 'Netflix Nigeria', 'Netflix Nigeria'),
    ('c04164be-c3ce-4adf-b4c6-5073a0ecc355', '22222222-2222-2222-2222-222222222222', 917.5, 'Glo Airtime', 'AirtimeTopup', '2025-07-12 00:00:00', '2025-07-12 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('a3edb838-ad45-40ce-ae3a-73cb1505c4e0', '22222222-2222-2222-2222-222222222222', 931.92, 'Glo Airtime', 'AirtimeTopup', '2025-07-19 00:00:00', '2025-07-19 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('862f9e67-9ab2-4479-b517-284f3215e2af', '22222222-2222-2222-2222-222222222222', 24866.94, 'Online Shopping', 'OTHER', '2025-07-22 00:00:00', '2025-07-22 00:00:00', 0, 0, 'Jumia', 'Jumia'),
    ('7345e57c-359a-468d-8456-bd0244f15045', '22222222-2222-2222-2222-222222222222', 560.17, 'Glo Airtime', 'AirtimeTopup', '2025-07-26 00:00:00', '2025-07-26 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('e3358c42-4a7b-48a7-9e8d-7ccf3a581415', '22222222-2222-2222-2222-222222222222', 15286.15, 'FairMoney Loan Repayment', 'LoanRepayment', '2025-07-28 00:00:00', '2025-07-28 00:00:00', 0, 0, 'FairMoney', 'FairMoney'),
    ('244d6ed4-7a3d-45df-8b32-76622d6c2ede', '22222222-2222-2222-2222-222222222222', 25250.98, 'AXA Mansard Insurance', 'InsurancePayment', '2025-08-01 00:00:00', '2025-08-01 00:00:00', 0, 0, 'AXA Mansard', 'AXA Mansard'),
    ('35d81f72-611a-407f-b21f-2255cb8a520f', '22222222-2222-2222-2222-222222222222', 741.22, 'Glo Airtime', 'AirtimeTopup', '2025-08-02 00:00:00', '2025-08-02 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('50560383-80ed-4e15-a356-057c93ce7a3b', '22222222-2222-2222-2222-222222222222', 546.56, 'Glo Airtime', 'AirtimeTopup', '2025-08-09 00:00:00', '2025-08-09 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('7af4d532-73bf-4c8c-849e-fa3a5407b7e4', '22222222-2222-2222-2222-222222222222', 2900.0, 'Netflix Subscription', 'DirectDebit', '2025-08-10 00:00:00', '2025-08-10 00:00:00', 0, 0, 'Netflix Nigeria', 'Netflix Nigeria'),
    ('239d1597-b74c-4646-831f-8f5999f90e9f', '22222222-2222-2222-2222-222222222222', 631.08, 'Glo Airtime', 'AirtimeTopup', '2025-08-16 00:00:00', '2025-08-16 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('976564c7-8df4-41a3-822e-8e440747e61a', '22222222-2222-2222-2222-222222222222', 827.05, 'Glo Airtime', 'AirtimeTopup', '2025-08-23 00:00:00', '2025-08-23 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('034d38cd-3c53-446b-839f-be78e278d9ee', '22222222-2222-2222-2222-222222222222', 15036.04, 'FairMoney Loan Repayment', 'LoanRepayment', '2025-08-28 00:00:00', '2025-08-28 00:00:00', 0, 0, 'FairMoney', 'FairMoney'),
    ('7c980f7e-045b-42c0-85d6-38d80106855b', '22222222-2222-2222-2222-222222222222', 508.91, 'Glo Airtime', 'AirtimeTopup', '2025-08-30 00:00:00', '2025-08-30 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('b98ffee9-1cda-43e2-b9ef-b7ae72b57090', '22222222-2222-2222-2222-222222222222', 25693.62, 'AXA Mansard Insurance', 'InsurancePayment', '2025-09-01 00:00:00', '2025-09-01 00:00:00', 0, 0, 'AXA Mansard', 'AXA Mansard'),
    ('1d67b309-6e6c-40c8-8d0a-4de73ea4dcc2', '22222222-2222-2222-2222-222222222222', 664.27, 'Glo Airtime', 'AirtimeTopup', '2025-09-06 00:00:00', '2025-09-06 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile'),
    ('16e31102-9986-43fa-97a5-f4c1bec9c5e5', '22222222-2222-2222-2222-222222222222', 3879.82, 'Online Shopping', 'OTHER', '2025-09-06 00:00:00', '2025-09-06 00:00:00', 0, 0, 'Jumia', 'Jumia'),
    ('91c746d3-4ff8-43ea-9c9d-c6f8ffea7a2a', '22222222-2222-2222-2222-222222222222', 2900.0, 'Netflix Subscription', 'DirectDebit', '2025-09-10 00:00:00', '2025-09-10 00:00:00', 0, 0, 'Netflix Nigeria', 'Netflix Nigeria'),
    ('802a89f8-7c4e-43e5-8c2b-e326a9a22b9a', '22222222-2222-2222-2222-222222222222', 763.81, 'Glo Airtime', 'AirtimeTopup', '2025-09-13 00:00:00', '2025-09-13 00:00:00', 0, 0, 'Glo Mobile', 'Glo Mobile');

    -- Get count of inserted transactions
    DECLARE @InsertedCount INT;
    SELECT @InsertedCount = COUNT(*) FROM Transactions;

    PRINT 'Successfully inserted ' + CAST(@InsertedCount AS VARCHAR(10)) + ' transactions.';

    -- =============================================
    -- Step 3: Verify the data
    -- =============================================
    PRINT 'Verifying transaction data...';

    SELECT
        UserId,
        COUNT(*) AS TransactionCount,
        MIN(TransactionDate) AS FirstTransaction,
        MAX(TransactionDate) AS LastTransaction,
        SUM(Amount) AS TotalAmount
    FROM Transactions
    GROUP BY UserId;

    -- Commit the transaction
    COMMIT TRANSACTION;
    PRINT 'Transaction seeding completed successfully!';

END TRY
BEGIN CATCH
    -- Rollback on error
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    -- Display error information
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Error occurred during transaction seeding:';
    PRINT 'Message: ' + @ErrorMessage;
    PRINT 'Severity: ' + CAST(@ErrorSeverity AS VARCHAR(10));
    PRINT 'State: ' + CAST(@ErrorState AS VARCHAR(10));

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

GO
