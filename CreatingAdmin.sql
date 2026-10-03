SELECT TOP (1000) [AdminId]
      ,[FirstName]
      ,[LastName]
      ,[Email]
      ,[Password]
      ,[UserType]
      ,[RegisteredOn]
  FROM [SKYTECH].[dbo].[SystemAdmins]

  insert into SystemAdmins
  values('Admin', 'System', 'admin@skytech.com', 'Fanele.23', 'SystemAdmin', GETDATE());