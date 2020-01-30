using TechTalk.SpecFlow.Parser;

namespace SpecFlow.Contrib.Variants.SpecFlowPlugin.Generator.ClassGenerator
{
    internal interface ITestClassGenerator
    {
        void CreateNamespace(string targetNameSpace);
        void CreateTestClassStructure(string testClassName, SpecFlowDocument document);
        void SetupTestClass();
        void SetupTestClassInitializeMethod();
        void SetupTestInitializeMethod();
        void SetupTestCleanupMethod();
        void SetupTestClassCleanupMethod();
    }
}
