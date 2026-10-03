-- =================================================================
-- SQL QUERIES FOR REPAIR SERVICE SYSTEM TESTING & DEBUGGING
-- =================================================================

-- 1. VIEW ALL PENDING REPAIR REQUESTS
SELECT 
    rr.RequestId,
    c.FirstName,
    c.LastName,
    rr.ItemName,
    rr.IssueDescription,
    ric.CategoryName,
    rr.Status,
    rr.DateCreated
FROM RepairRequests rr
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
INNER JOIN RepairItemCategories ric ON rr.CategoryId = ric.CategoryId
WHERE rr.Status = 0  -- 0 = Pending
ORDER BY rr.DateCreated DESC;

-- 2. VIEW ASSIGNED REQUESTS WITH TECHNICIAN INFO
SELECT 
    rr.RequestId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    rr.ItemName,
    t.TechnicianFirstName + ' ' + t.TechnicianLastName AS TechnicianName,
    ra.AssignedDate,
    rr.Status,
    ra.AssignmentNotes
FROM RepairRequests rr
INNER JOIN RepairAssignments ra ON rr.RequestId = ra.RepairRequestId
INNER JOIN Technicians t ON ra.TechnicianId = t.TechnicianId
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
WHERE rr.Status = 1  -- 1 = Assigned
ORDER BY ra.AssignedDate DESC;

-- 3. VIEW QUOTATIONS WITH DETAILS
SELECT 
    q.QuotationId,
    rr.RequestId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    rr.ItemName,
    q.Status,
    q.TotalCost,
    q.CreatedDate,
    COUNT(qi.QuotationItemId) AS NumberOfItems
FROM Quotations q
INNER JOIN RepairRequests rr ON q.RepairRequestId = rr.RequestId
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
LEFT JOIN QuotationItems qi ON q.QuotationId = qi.QuotationId
GROUP BY q.QuotationId, rr.RequestId, c.FirstName, c.LastName, rr.ItemName, 
         q.Status, q.TotalCost, q.CreatedDate
ORDER BY q.CreatedDate DESC;

-- 4. VIEW QUOTATION ITEMS FOR A SPECIFIC QUOTATION
SELECT 
    qi.QuotationItemId,
    qi.ItemDescription,
    qi.UnitCost,
    qi.Quantity,
    (qi.UnitCost * qi.Quantity) AS LineTotal
FROM QuotationItems qi
WHERE qi.QuotationId = @QuotationId
ORDER BY qi.QuotationItemId;

-- 5. VIEW REQUEST TRACKING HISTORY
SELECT 
    rt.TrackingId,
    rr.RequestId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    rt.Status,
    rt.StatusMessage,
    rt.UpdatedDate
FROM RepairTrackings rt
INNER JOIN RepairRequests rr ON rt.RepairRequestId = rr.RequestId
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
WHERE rr.RequestId = @RequestId
ORDER BY rt.UpdatedDate DESC;

-- 6. VIEW AVAILABLE TECHNICIANS BY CATEGORY
SELECT 
    t.TechnicianId,
    t.TechnicianFirstName + ' ' + t.TechnicianLastName AS TechnicianName,
    ric.CategoryName,
    t.Status
FROM Technicians t
INNER JOIN RepairItemCategories ric ON t.CategoryId = ric.CategoryId
WHERE t.Status = 0  -- 0 = Available
ORDER BY ric.CategoryName, t.TechnicianFirstName;

-- 7. VIEW BUSY TECHNICIANS (CURRENTLY ASSIGNED)
SELECT 
    t.TechnicianId,
    t.TechnicianFirstName + ' ' + t.TechnicianLastName AS TechnicianName,
    ric.CategoryName,
    rr.ItemName,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    ra.AssignedDate
FROM Technicians t
INNER JOIN RepairItemCategories ric ON t.CategoryId = ric.CategoryId
INNER JOIN RepairAssignments ra ON t.TechnicianId = ra.TechnicianId
INNER JOIN RepairRequests rr ON ra.RepairRequestId = rr.RequestId
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
WHERE t.Status = 2  -- 2 = Busy
ORDER BY ra.AssignedDate DESC;

-- 8. VIEW COMPLETED REPAIRS
SELECT 
    rr.RequestId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    rr.ItemName,
    t.TechnicianFirstName + ' ' + t.TechnicianLastName AS TechnicianName,
    q.TotalCost,
    rc.FinalAmount,
    rr.DateCreated,
    rc.IssuedDate
FROM RepairRequests rr
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
LEFT JOIN RepairAssignments ra ON rr.RequestId = ra.RepairRequestId
LEFT JOIN Technicians t ON ra.TechnicianId = t.TechnicianId
LEFT JOIN Quotations q ON rr.RequestId = q.RepairRequestId
LEFT JOIN Receipts rc ON rr.RequestId = rc.RepairRequestId
WHERE rr.Status = 4  -- 4 = Completed
ORDER BY rc.IssuedDate DESC;

-- 9. VIEW REPAIR REQUEST STATISTICS
SELECT 
    ric.CategoryName,
    COUNT(rr.RequestId) AS TotalRequests,
    SUM(CASE WHEN rr.Status = 0 THEN 1 ELSE 0 END) AS PendingRequests,
    SUM(CASE WHEN rr.Status = 1 THEN 1 ELSE 0 END) AS AssignedRequests,
    SUM(CASE WHEN rr.Status = 4 THEN 1 ELSE 0 END) AS CompletedRequests,
    ISNULL(AVG(q.TotalCost), 0) AS AvgQuotationCost
FROM RepairRequests rr
INNER JOIN RepairItemCategories ric ON rr.CategoryId = ric.CategoryId
LEFT JOIN Quotations q ON rr.RequestId = q.RepairRequestId
GROUP BY ric.CategoryName
ORDER BY TotalRequests DESC;

-- 10. VIEW CUSTOMER REPAIR HISTORY
SELECT 
    rr.RequestId,
    rr.ItemName,
    ric.CategoryName,
    rr.Status,
    rr.DateCreated,
    ISNULL(q.TotalCost, 0) AS QuotedPrice,
    ISNULL(rc.FinalAmount, 0) AS PaidAmount
FROM RepairRequests rr
INNER JOIN RepairItemCategories ric ON rr.CategoryId = ric.CategoryId
LEFT JOIN Quotations q ON rr.RequestId = q.RepairRequestId
LEFT JOIN Receipts rc ON rr.RequestId = rc.RepairRequestId
WHERE rr.CustomerId = @CustomerId
ORDER BY rr.DateCreated DESC;

-- 11. VIEW TECHNICIAN WORKLOAD
SELECT 
    t.TechnicianId,
    t.TechnicianFirstName + ' ' + t.TechnicianLastName AS TechnicianName,
    COUNT(DISTINCT ra.RepairRequestId) AS TotalAssignments,
    SUM(CASE WHEN rr.Status = 1 THEN 1 ELSE 0 END) AS CurrentWorkload,
    SUM(CASE WHEN rr.Status = 4 THEN 1 ELSE 0 END) AS CompletedJobs,
    AVG(q.TotalCost) AS AvgJobCost
FROM Technicians t
LEFT JOIN RepairAssignments ra ON t.TechnicianId = ra.TechnicianId
LEFT JOIN RepairRequests rr ON ra.RepairRequestId = rr.RequestId
LEFT JOIN Quotations q ON rr.RequestId = q.RepairRequestId
GROUP BY t.TechnicianId, t.TechnicianFirstName, t.TechnicianLastName
ORDER BY CurrentWorkload DESC;

-- 12. VIEW APPROVED BUT NOT YET IN PROGRESS
SELECT 
    rr.RequestId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    rr.ItemName,
    q.TotalCost,
    q.Status AS QuotationStatus,
    q.CreatedDate
FROM RepairRequests rr
INNER JOIN Customers c ON rr.CustomerId = c.CustomerId
INNER JOIN Quotations q ON rr.RequestId = q.RepairRequestId
WHERE q.Status = 1  -- 1 = Approved
  AND rr.Status = 2  -- 2 = Quoted (not yet in progress)
ORDER BY q.CreatedDate;

-- NOTES:
-- Status Values: 0=Pending, 1=Assigned, 2=Qouted, 3=InProgress, 4=Completed, 5=Cancelled
-- QuotationStatus: 0=Pending, 1=Approved, 2=Rejected, 3=Expired
-- TechnicianAvailability: 0=Available, 1=Unavailable, 2=Busy

-- USEFUL PARAMETERS:
-- Replace @RequestId with actual request ID (e.g., 5)
-- Replace @CustomerId with actual customer ID (e.g., 1)
-- Replace @QuotationId with actual quotation ID (e.g., 3)
