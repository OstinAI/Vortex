using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vortex
{
    public static class ApiConfig
    {
        // 🔹 Главный базовый адрес сервера
        // Пока локальный, потом просто поменяешь на домен
        public static string BaseUrl = "http://127.0.0.1:5000";

        // 🔹 Готовые ссылки на основные эндпоинты
        public static string AuthLogin => $"{BaseUrl}/api/auth/login";
        public static string EmployeesList => $"{BaseUrl}/api/employees/list";
        public static string EmployeesCreate => $"{BaseUrl}/api/employees/create";
        public static string EmployeesUpdate => $"{BaseUrl}/api/employees/update";
        public static string EmployeesDelete => $"{BaseUrl}/api/employees/delete";
        // 🔹 Updates
        public static string UpdateCheck => $"{BaseUrl}/api/update/check";
        public static string UpdateDownload(string file)
            => $"{BaseUrl}/api/update/download/{file}";
        // сюда добавляешь остальные по мере необходимости
    }
}

