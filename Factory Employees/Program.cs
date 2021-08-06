using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Factory_Employees
{
    enum EmployeeType { Salaried, Commission, Hourly}
    class Employee
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public EmployeeType EmployeeType { get; private set; }

        public Employee(int id, string name, EmployeeType employeeType)
        {
            Id = id;
            Name = name;
            EmployeeType = employeeType;
        }
    }

    abstract class PaymentProcessorBase
    {
        public abstract void ProcessPayment(Employee employee);
    }

    class SalariedPaymentProcessor : PaymentProcessorBase
    {
        public override void ProcessPayment(Employee employee)
        {
            Console.WriteLine(employee.Name + "'s Payment was processed using Salaried Payment Processor");
        }
    }

    class CommissionPaymentProcessor : PaymentProcessorBase
    {
        public override void ProcessPayment(Employee employee)
        {
            Console.WriteLine(employee.Name + "'s Payment was processed using Commission Payment Processor");
        }
    }

    class HourlyPaymentProcessor : PaymentProcessorBase
    {
        public override void ProcessPayment(Employee employee)
        {
            Console.WriteLine(employee.Name + "'s Payment was processed using Hourly Payment Processor");
        }
    }

    class SimplePaymentProcessorFactory
    {
        public PaymentProcessorBase CreateFactory(string identifier)
        {
            if (identifier == "SalariedPaymentProcessor")
                return new SalariedPaymentProcessor();
            else if (identifier == "CommissionPaymentProcessor")
                return new CommissionPaymentProcessor();
            else if (identifier == "HourlyPaymentProcessor")
                return new HourlyPaymentProcessor();
            throw new ArgumentException("The identifier: " + identifier + ", is not a valid identifier", identifier);
        }
    }

    static class PaymentProcessorFactory
    {
        private static Type classType = typeof(PaymentProcessorBase);
        private static Type[] constructorArgs = new Type[] { };

        private static readonly ConcurrentDictionary<string, Type> classRegistry = new ConcurrentDictionary<string, Type>();
        private static readonly ConcurrentDictionary<string, ConstructorDelegate> classConstructors = new ConcurrentDictionary<string, ConstructorDelegate>();

        delegate PaymentProcessorBase ConstructorDelegate();

        static PaymentProcessorFactory()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var paymentProcessors = from b in Assembly.GetEntryAssembly().GetTypes()
                                    where b.IsSubclassOf(classType)
                                    select b;

            foreach (var type in paymentProcessors)
                classRegistry.TryAdd(type.Name, type);
        }


        public static PaymentProcessorBase Create(string identifier)
        {
            if (String.IsNullOrEmpty(identifier))
                throw new ArgumentException("identifier can not be null or empty", identifier);
            if (!classRegistry.ContainsKey(identifier))
                throw new ArgumentException("No PaymentProcessor has been registered with the identifier: " + identifier);

            return Create(classRegistry[identifier]);
        }

        private static PaymentProcessorBase Create(Type type)
        {
            ConstructorDelegate del;

            if (classConstructors.TryGetValue(type.Name, out del))
                return del();

            DynamicMethod dynamicMethod = new DynamicMethod("CreateInstance", type, constructorArgs, classType);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Newobj, type.GetConstructor(constructorArgs));
            ilGenerator.Emit(OpCodes.Ret);

            del = (ConstructorDelegate)dynamicMethod.CreateDelegate(typeof(ConstructorDelegate));
            classConstructors.TryAdd(type.Name, del);
            return del();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var employees = new List<Employee>
            {
                new Employee(1, "Employee One", EmployeeType.Commission),
                new Employee(2, "Employee Two", EmployeeType.Hourly),
                new Employee(3, "Employee Three", EmployeeType.Salaried),
                new Employee(4, "Employee Four", EmployeeType.Salaried)
            };

            PaymentProcessorBase processor = null;

            foreach (var employee in employees)
            {
                switch (employee.EmployeeType)
                {
                    case EmployeeType.Salaried:
                        processor = new SalariedPaymentProcessor();
                        break;
                    case EmployeeType.Commission:
                        processor = new CommissionPaymentProcessor();
                        break;
                    case EmployeeType.Hourly:
                        processor = new HourlyPaymentProcessor();
                        break;
                }
                processor.ProcessPayment(employee);
            }

        }
    }
}
