To test:
1. dotnet run on VsCode
2. use Postman with header x-API-key admin-api-key-99999
3. Test with Get http://localhost:5199/api/user/
   or http://localhost:5199/api/user/{id}
   or Test with POST http://localhost:5199/api/user/
   with BODY => raw

{
    "name": "Abc",
    "salary": 50000,
    "department": "IT"
}

