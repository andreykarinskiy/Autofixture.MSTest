using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using AutoFixture.Kernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoFixture.MSTest
{
    public class AutoDataAttribute : Attribute, ITestDataSource
    {
        private readonly Lazy<IFixture> fixtureLazy;

        private IFixture Fixture => fixtureLazy.Value;

        /// <summary>
        /// Construct a <see cref="AutoDataAttribute"/>.
        /// </summary>
        public AutoDataAttribute() : this(() => new Fixture())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoDataAttribute"/> class
        /// with the supplied <paramref name="fixtureFactory"/>. Fixture will be created
        /// on demand using the provided factory.
        /// </summary>
        /// <param name="fixtureFactory">The fixture factory used to construct the fixture.</param>
        protected AutoDataAttribute(Func<IFixture> fixtureFactory)
        {
            if (fixtureFactory == null)
            {
                throw new ArgumentNullException(nameof(fixtureFactory));
            }

            fixtureLazy = new Lazy<IFixture>(fixtureFactory, LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Gets or sets display name in test results for customization.
        /// </summary>
        public string DisplayName { get; set; }

        public IEnumerable<object[]> GetData(MethodInfo method)
        {
            yield return BuildFrom(method);
        }

        public string GetDisplayName(MethodInfo method, object[] data)
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return DisplayName;
            }

            if (data != null)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", method.Name, string.Join(",", data));
            }

            return null;
        }

        public object[] BuildFrom(MethodInfo method)
        {
            return method
                .GetParameters()
                .Select(Resolve)
                .ToArray();
        }

        private object Resolve(ParameterInfo parameter)
        {
            CustomizeFixtureByParameter(parameter);

            return new SpecimenContext(Fixture)
                .Resolve(parameter);
        }

        private void CustomizeFixtureByParameter(ParameterInfo parameter)
        {
            var customizeAttributes = parameter
                .GetCustomAttributes<Attribute>(false)
                .OfType<IParameterCustomizationSource>()
                .OrderBy(x => x, new CustomizeAttributeComparer());

            foreach (var ca in customizeAttributes)
            {
                var customization = ca.GetCustomization(parameter);
                Fixture.Customize(customization);
            }
        }
    }
}
