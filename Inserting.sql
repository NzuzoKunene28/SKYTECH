select * from StaffPositions
  select * from Branches
  select * from Roles
  select * from shopowners

  select * from Staffs
  --ADMIN
  insert into SystemAdmins
values('Fanele', 'Dhladhla', 'admin@gmail.com', 'Fanele.23', 'SystemAdmin', GETDATE());

--HR
select * from HRs;
insert into Hrs
values('Nzuzo', 'Kunene', 'hr@gmail.com', 'HR', 'Fanele.23')



--DEPARTMENTS
 select * from Departments;
 insert into Departments
 values('Sales Floor', 1), ('Management', 1), ('Technical Support', 1), ('Cash Desk', 1);

 --ADMIN
 select * from SystemAdmins;
  select * from Branches;

--STAFF POSITIONS
select * from StaffPositions;
insert into StaffPositions
values('Cleaner', 30, 1), ('Supervisor', 30, 2), ('Technician', 30, 3), ('Cashier', 30, 4);

--Roles
select * from Roles;
insert into Roles
values('Employee'), ('ShopOwner'), ('HR');





 drop database SKYTECH