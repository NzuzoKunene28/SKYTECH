  use SKYTECH
  --branch values
  insert into Branches
  values('Skytech_B', 'Empangeni');

  --Department Values
    INSERT INTO Departments (DepartmentName)
VALUES 
    ('Technical Department'),
    ('Sales Department'),
    ('Operation Department'),
    ('Cash Desk Department'),
    ('Human Resources Department');

    --Technical department values
    insert into StaffPositions
    values('IT Support Specialist', 1),
    ('Software Developer', 1);

    --Sales department
    insert into StaffPositions
    values('Account Manager', 2),
    ('Sales Analyst', 2);
   

    --Operation Deparment
    insert into StaffPositions
    values('Cleaner', 3),
    ('Logistic Coordinator', 3);

    --Cash Desk Dep
    insert into StaffPositions
    values('Cashier', 4),
    ('Cashier Supervisor', 4);

    --HR dep
    insert into StaffPositions
    values('Recruitment Specialist', 5);


    --HR login details
    insert into HRs
    values('Andiswa', 'Dhlamini','hr@gmail.com','HR', 'Fanele.23' );

   
   insert into Roles 
   values ('Employee'),('ShopOwner'),('SystemAdmin'),('Driver'),('HR')