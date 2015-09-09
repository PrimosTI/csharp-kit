/*

GenericRazor para .Net
Copyright (c) 2015 Primos Tecnologia da Informação Ltda.

É permitida a distribuição irrestrita desta obra, livre de taxas e encargos,
incluindo, e sem limitar-se ao uso, cópia, modificação, combinação e/ou
publicação, bem como a aplicação em outros trabalhos derivados deste.

A mensagem de direito autoral:
    GenericRazor para .Net
    Copyright (c) 2015 Primos Tecnologia da Informação Ltda.
deverá ser incluída em todas as cópias ou partes do obra derivado que permitam
a inclusão desta informação.

O autor desta obra poderá alterar as condições de licenciamento em versões
futuras, porém, tais alterações serão vigentes somente a partir da versão
alterada.

O LICENCIANTE OFERECE A OBRA “NO ESTADO EM QUE SE ENCONTRA” (AS IS) E NÃO
PRESTA QUAISQUER GARANTIAS OU DECLARAÇÕES DE QUALQUER ESPÉCIE RELATIVAS À ESTA
OBRA, SEJAM ELAS EXPRESSAS OU IMPLÍCITAS, DECORRENTES DA LEI OU QUAISQUER
OUTRAS, INCLUINDO, SEM LIMITAÇÃO, QUAISQUER GARANTIAS SOBRE A TITULARIDADE,
ADEQUAÇÃO PARA QUAISQUER PROPÓSITOS, NÃO-VIOLAÇÃO DE DIREITOS, OU INEXISTÊNCIA
DE QUAISQUER DEFEITOS LATENTES, ACURACIDADE, PRESENÇA OU AUSÊNCIA DE ERROS,
SEJAM ELES APARENTES OU OCULTOS. REVOGAM-SE AS PERMISSÕES DESTA LICENÇA EM
JURISDIÇÕES QUE NÃO ACEITEM A EXCLUSÃO DE GARANTIAS IMPLÍCITAS.

EM NENHUMA CIRCUNSTÂNCIA O LICENCIANTE SERÁ RESPONSÁVEL PARA COM VOCÊ POR
QUAISQUER DANOS, ESPECIAIS, INCIDENTAIS, CONSEQÜENCIAIS, PUNITIVOS OU
EXEMPLARES, ORIUNDOS DESTA LICENÇA OU DO USO DESTA OBRA, MESMO QUE O
LICENCIANTE TENHA SIDO AVISADO SOBRE A POSSIBILIDADE DE TAIS DANOS.

[PT]
*** AVISO LEGAL ***
Antes de usar esta obra, você deve entender e concordar com os termos acima.

[ES]
*** AVISO LEGAL ***
Antes de usar este trabajo, debe entender y aceptar las condiciones anteriores.

[EN]
*** LEGAL NOTICE ***
You must understand and agree to the above terms before using this work.

[CH]
*** 法律聲明 ***
使用這項工作之前，了解並同意本許可。

[JP]
*** 法律上の注意事項 ***
この作品を使用する前に、理解し、このライセンスに同意する。

*/
/*
    =====================================
    GenericRazor para .Net
    =====================================
    Versão:      0.0.1
    Criação:     2015-08-23
    Alteração:   2015-08-23
    
    Escrito por: Rodrigo Speller
    E-mail:      rspeller@primosti.com.br
    -------------------------------------

"GenericRazor para .Net" é uma engine que permite o uso da sintaxe do ASP.NET
Razor da Microsoft como processador de templates personalizadas, permitindo a
captura da saída.

Alterações
----------
» 0.0.1
- Lançamento para testes.

*/
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

using GENERATOR_RESULTS = System.Web.Razor.GeneratorResults;
using RAZOR_TEMPLATE_ENGINE = System.Web.Razor.RazorTemplateEngine;
using RAZOR_ENGINE_HOST = System.Web.Razor.RazorEngineHost;
using CSSHARP_RAZOR_CODE_LANGUAGE = System.Web.Razor.CSharpRazorCodeLanguage;

namespace GenericRazor
{

    #region Interfaces

    /// <summary>
    /// Interface para definição de classe de configuração para o RazorEngine.
    /// Essas definições determinam os parâmetros para a operação do RazorEngine.
    /// </summary>        
    public interface IRazorEngineConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        bool Debug { get; set; }

        /// <summary>
        /// Define se os assemblies devem ser compilados em disco ou na memória.
        /// </summary>
        bool GenerateInMemory { get; set; }

        /// <summary>
        /// Define o caminho do diretório para armazenar os arquivos temporários
        /// gerados.
        /// Os arquivos serão armazenados em disco somente se a propriedade
        /// <see cref="GenerateInMemory"/> for false.
        /// </summary>
        string TempPath { get; set; }
    }

    /// <summary>
    /// Interface para a definição de classe que instancia um objeto chave para um template.
    /// </summary>
    public interface ITemplateKey { }

    #endregion

    /// <summary>
    /// Classe principal para a execução dos modelos.
    /// </summary>
    /// <typeparam name="TTemplate">
    /// Um tipo derivado de <see cref="RazorTemplateBase"/> que determina o tipo
    /// de template que a instância do RazorEngine irá manipular.
    /// </typeparam>
    public class RazorEngine<TTemplate>
        where TTemplate : RazorTemplateBase, new()
    {
        private sealed class TemplateKey : ITemplateKey { }

        public IRazorEngineConfiguration Configuration { get; private set; }

        /// <summary>
        /// Lista de namespaces a serem importadas.
        /// </summary>
        public List<string> NamespaceImports { get; private set; }

        /// <summary>
        /// Lista de Assemblies a serem referenciadas.
        /// </summary>
        public List<string> ReferencedAssemblies { get; private set; }

        protected Dictionary<ITemplateKey, Type> TemplateCache { get; private set; }

        public RazorEngine() : this(new RazorEngineConfiguration()) { }

        public RazorEngine(IRazorEngineConfiguration configuration)
        {
            if (configuration == null)
                configuration = new RazorEngineConfiguration();

            Configuration = configuration;

            TemplateCache = new Dictionary<ITemplateKey, Type>();

            // namespaces precarregadas
            NamespaceImports = new List<string>() { 
				"System",
				"System.Collections.Generic",
				"System.IO",
				"System.Linq"
				/* ,
				"System.Net",
				"System.Web",
				"System.Web.Helpers",
				"System.Web.Security",
				"System.Web.UI",
				"System.Web.WebPages",
				"System.Web.WebPages.Html"
				 */
            };

            // assemblies precarregadas
            ReferencedAssemblies = new List<string>() {
				"System.dll",
				"System.Core.dll",
				"Microsoft.CSharp.dll"
			};
        }

        protected RAZOR_TEMPLATE_ENGINE CreateRazorTemplateEngine()
        {
            RAZOR_ENGINE_HOST host = new RAZOR_ENGINE_HOST(new CSSHARP_RAZOR_CODE_LANGUAGE())
            {
                DefaultBaseClass = typeof(TTemplate).FullName,
                DefaultClassName = null,
                DefaultNamespace = null
            };

            foreach (string ns in this.NamespaceImports.Distinct())
                host.NamespaceImports.Add(ns);

            return new RAZOR_TEMPLATE_ENGINE(host);
        }

        public ITemplateKey Compile(TextReader input)
        {
            return Compile(new TextReader[] { input }, null as string[])[0];
        }

        public ITemplateKey Compile(string file)
        {
            return Compile(null as TextReader[], new string[] { file })[0];
        }

        public ITemplateKey[] Compile(params string[] files)
        {
            if (files == null)
                throw new ArgumentNullException("files");

            if (files.Distinct().Count() != files.Length)
                throw new ArgumentException(null /*TODO*/, "files");

            return Compile(null as TextReader[], files);
        }

        public ITemplateKey[] Compile(TextReader[] inputs, string[] files)
        {
            // ARGS

            if (inputs == null)
                if (files == null)
                    return new ITemplateKey[0];
                else
                    inputs = new TextReader[files.Length];
            else if (files == null)
                files = new string[inputs.Length];

            if (files.Length > inputs.Length)
            {
                var newInput = new TextReader[files.Length];
                Array.Copy(inputs, newInput, inputs.Length);
                inputs = newInput;
            }
            else if (inputs.Length > files.Length)
            {
                var newFiles = new string[inputs.Length];
                Array.Copy(files, newFiles, files.Length);
                files = newFiles;
            }

            var count = inputs.Length;

            if (count == 0)
                return new ITemplateKey[0];

            // VARS

            var debug = Configuration.Debug;

            string[] sourceCode = debug ? new string[count] : null; //debug

            string guid = null;
            string assemblyName = null;
            string tempPath = null;

            Action random = () =>
            {
                guid = Guid.NewGuid().ToString("N");
                assemblyName = "RazorTemplate_" + guid;
                tempPath = Path.Combine(Configuration.TempPath, assemblyName);
            };

            random();

            if (debug || !Configuration.GenerateInMemory)
            {
                while (Directory.Exists(tempPath))
                    random();

                Directory.CreateDirectory(tempPath);
            }

            // PARSE

            RAZOR_TEMPLATE_ENGINE engine = CreateRazorTemplateEngine();

            var codeDom = new System.CodeDom.CodeCompileUnit[count];

            for (int i = 0; i < count; i++)
            {
                var input = inputs[i];
                var file = files[i];
                var className = "Template" + i.ToString("D");

                if (input == null)
                    if (file == null)
                        continue;
                    else
                        input = new StreamReader(file);

                // debug
                if (debug)
                {
                    try
                    {
                        sourceCode[i] = input.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        throw new RazorTemplateCompileException<TTemplate>(
                            this
                            ,
                            file == null
                                ? Resources.TemplateReadingError
                                : string.Format(Resources.TemplateFileReadingError, file)
                            ,
                            ex
                        );
                    }

                    input = new StringReader(sourceCode[i]);

                    if (file == null)
                    {
                        file = NormalizePath(Path.Combine(tempPath, className + ".cshtml"));

                        using (var writer = File.CreateText(file))
                            writer.Write(sourceCode[i]);

                        files[i] = file;
                    }
                    else
                    {
                        files[i] = NormalizePath(file);
                    }
                }

                codeDom[i] = engine.GenerateCode(input, className, assemblyName, debug ? file : null).GeneratedCode;
            }

            // COMPILE

            var ReferencedAssemblies = new List<string>(this.ReferencedAssemblies);
            // Adiciona o assembly do tipo do template, para que o tipo definido por TTemplate esteja disponível
            ReferencedAssemblies.Add(typeof(TTemplate).Assembly.Location);

            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CompilerParameters compilerParameters = new CompilerParameters(ReferencedAssemblies.Distinct().ToArray());

            #region compilerParameters setup

            compilerParameters.GenerateInMemory = !debug && Configuration.GenerateInMemory;

            if (!compilerParameters.GenerateInMemory)
                compilerParameters.OutputAssembly = Path.Combine(tempPath, assemblyName + ".dll");

            if (debug)
            {
                compilerParameters.IncludeDebugInformation = debug;
                compilerParameters.CompilerOptions = "/optimize";
                compilerParameters.TempFiles = new TempFileCollection(tempPath, true);
            }

            #endregion

            CompilerResults compilerResults = codeProvider.CompileAssemblyFromDom(compilerParameters, codeDom);

            // COMPILE ERROR

            if (compilerResults.Errors.Count > 0)
            {
                if (!debug)
                    throw new RazorTemplateCompileException<TTemplate>(this, null, null);

                var filesErrors = new Dictionary<string, List<CompilerError>>(compilerResults.Errors.Count);

                List<CompilerError> fileErrors;
                foreach (CompilerError error in compilerResults.Errors)
                {
                    var file = NormalizePath(error.FileName);
                    if (filesErrors.TryGetValue(file, out fileErrors))
                        fileErrors.Add(error);
                    else
                        filesErrors.Add(file, new List<CompilerError>() { error });
                }

                var generatedCode = codeDom.Select(dom =>
                {
                    using (StringWriter writer = new StringWriter())
                    {
                        codeProvider.GenerateCodeFromCompileUnit(dom, writer, new CodeGeneratorOptions());
                        return writer.ToString();
                    }
                }).ToArray();

                var entries = new List<RazorTemplateCompileInfo>(count);
                for (var i = 0; i < count; i++)
                {
                    if (!filesErrors.TryGetValue(files[i], out fileErrors))
                        fileErrors = new List<CompilerError>();

                    var entry = new RazorTemplateCompileInfo
                    {
                        CodeDom = codeDom[i],
                        FileName = files[i],
                        GeneratedCode = generatedCode[i],
                        SourceCode = sourceCode[i],
                        Errors = fileErrors as IReadOnlyList<CompilerError>
                    };

                    entries.Add(entry);
                }

                throw new RazorDebuggableTemplateCompileException<TTemplate>(this, entries, null, null);
            }

            // RETURN

            Assembly assembly = compilerResults.CompiledAssembly;

            var keys = new ITemplateKey[count];

            for (int i = 0; i < count; i++)
            {
                var key = new TemplateKey();
                keys[i] = key;

                TemplateCache.Add(key, assembly.GetType(assemblyName + ".Template" + i.ToString("D")));
            }

            return keys;
        }

        private static string NormalizePath(string path)
        {
            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            var root = new Uri(new Uri(Path.GetPathRoot(path)).LocalPath.ToUpperInvariant());
            return new Uri(root, root.MakeRelativeUri(new Uri(path))).LocalPath;
        }

        public void Run(ITemplateKey key, TextWriter output)
        {
            if (key == null)
                return;

            using (TTemplate template = CreateTemplate(key))
            {
                template.writer = output;

                if (Configuration.Debug)
                {
                    ExecuteTemplate(template);
                }
                else
                {
                    try
                    {
                        ExecuteTemplate(template);
                    }
                    catch (Exception ex)
                    {
                        throw new RazorTemplateExecutionException<TTemplate>(this, key, template, null, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Compila e executa um template em um TextReader, escrevendo o resultado da execução em um objeto TextWriter.
        /// </summary>
        /// <param name="input">TextReader contendo o conteúdo do template a ser compilado e executado. O TextReader não é fechado após a execução.</param>
        /// <param name="output">TextWriter que será escrita a saída durante a execução. O TextWriter não é fechado após a execução.</param>
        /// <returns>Retorna um objeto <see cref="ITemplateKey"/> que aponta para o template compilado. Este template pode ser executado novamento chamando o método <see cref="Run"/></returns>
        public ITemplateKey CompileRun(TextReader input, TextWriter output)
        {
            var key = Compile(input);

            Run(key, output);

            return key;
        }

        /// <summary>
        /// Compila e executa um template em um arquivo, escrevendo o resultado da execução em um objeto TextWriter.
        /// </summary>
        /// <param name="input">Caminho do arquivo contendo o conteúdo do template a ser compilado e executado.</param>
        /// <param name="output">TextWriter que será escrita a saída durante a execução. O TextWriter não é fechado após a execução.</param>
        /// <returns>Retorna um objeto <see cref="ITemplateKey"/> que aponta para o template compilado. Este template pode ser executado novamento chamando o método <see cref="Run"/></returns>
        public ITemplateKey CompileRun(string file, TextWriter output)
        {
            var key = Compile(file);

            Run(key, output);

            return key;
        }

        private TTemplate CreateTemplate(ITemplateKey key)
        {
            Type templateType = null;

            if (!TemplateCache.TryGetValue(key, out templateType) || templateType == null)
                throw new RazorException(Resources.CompiledTemplateNotFound);

            TTemplate template = null;
            try
            {
                template = Activator.CreateInstance(templateType) as TTemplate;
            }
            catch (Exception ex)
            {
                throw new RazorTemplateCreationException<TTemplate>(
                    this,
                    key,
                    templateType,
                    null,
                    ex
                );
            }

            if (template == null)
                throw new RazorTemplateCreationException<TTemplate>(
                    this,
                    key,
                    templateType,
                    string.Format(Resources.TemplateInstanceError, Resources.UnableToCreateTemplate),
                    null
                );

            OnCreateTemplate(template);

            return template;
        }

        protected virtual void OnCreateTemplate(TTemplate template) { }

        private void ExecuteTemplate(TTemplate template)
        {
            template.Execute();
        }
    }

    /// <summary>
    /// Configuração para o RazorEngine. Essas definições determinam os
    /// parâmetros para a operação do RazorEngine.
    /// </summary>        
    public class RazorEngineConfiguration : IRazorEngineConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Define se os assemblies devem ser compilados em disco ou na memória.
        /// </summary>
        public bool GenerateInMemory
        {
            get { return !Debug && generateInMemory; }
            set { generateInMemory = value; }
        }
        private bool generateInMemory = true;

        /// <summary>
        /// Define o caminho do diretório para armazenar os arquivos temporários
        /// gerados.
        /// Os arquivos serão armazenados em disco somente se a propriedade
        /// <see cref="GenerateInMemory"/> for false.
        /// </summary>
        public string TempPath
        {
            get
            {
                if (tempPath == null)
                    return Path.GetTempPath();
                return tempPath;
            }
            set
            {

                if (!string.IsNullOrEmpty(value))
                    value = null;
                tempPath = value;
            }
        }
        private string tempPath = null;
    }

    public class RazorTemplateBase : IDisposable
    {
        internal TextWriter writer;

        public RazorTemplateBase() { }

        public RazorTemplateBase(TextWriter writer)
        {
            this.writer = writer;
        }

        public void Write() { }

        public virtual void Write(object value)
        {
            if (this.writer != null)
                writer.Write(value);
        }

        public virtual void WriteLiteral(object value)
        {
            if (this.writer != null)
                writer.Write(value);
        }

        public virtual void Execute() { }

        public virtual void Dispose() { }
    }

    public static class RazorStringExtension
    {
        public static ITemplateKey CompileString<TTemplate>(this RazorEngine<TTemplate> engine, string source)
            where TTemplate : RazorTemplateBase, new()
        {
            //TODO: cache
            using (var reader = new StringReader(source))
                return engine.Compile(reader);
        }

        public static string RunToString<TTemplate>(this RazorEngine<TTemplate> engine, ITemplateKey key)
            where TTemplate : RazorTemplateBase, new()
        {
            using (var writer = new StringWriter())
            {
                engine.Run(key, writer);
                return writer.ToString();
            }
        }

        public static string CompileRunString<TTemplate>(this RazorEngine<TTemplate> engine, string source)
            where TTemplate : RazorTemplateBase, new()
        {
            return engine.RunToString(engine.CompileString(source));
        }
    }

    #region Exception

    public class RazorException : Exception
    {
        public RazorException() { }

        public RazorException(string message) : this(null, null) { }

        public RazorException(string message, Exception innerException)
            : base(message, innerException)
        { }

        private string message;
        public override string Message
        {
            get
            {
                return (message == null)
                    ? base.Message
                    : message;
            }
        }

        protected bool HasCustomMessage { get { return message != null; } }

        protected void SetMessage(string value) { message = value; }

        public override string ToString() { return Message; }
    }

    public class RazorTemplateExecutionException<TTemplate> : RazorException
        where TTemplate : RazorTemplateBase, new()
    {
        public RazorTemplateExecutionException(RazorEngine<TTemplate> engine, ITemplateKey key, TTemplate template, string message, Exception innerException)
            : base(message, innerException)
        {
            Engine = engine;
            TemplateKey = key;
            Template = template;
        }

        public RazorEngine<TTemplate> Engine { get; private set; }

        public ITemplateKey TemplateKey { get; private set; }

        public TTemplate Template { get; private set; }

        public override string Message
        {
            get
            {
                if (HasCustomMessage)
                    return base.Message;

                var innerMessage = InnerException == null
                    ? Resources.UnspecifiedError
                    : InnerException.Message
                ;

                return string.Format(Resources.TemplateExecutionError, innerMessage);
            }
        }
    }

    public class RazorTemplateCreationException<TTemplate> : RazorException
            where TTemplate : RazorTemplateBase, new()
    {
        public RazorTemplateCreationException(RazorEngine<TTemplate> engine, ITemplateKey key, Type templateType, string message, Exception innerException)
            : base(message, innerException)
        {
            Engine = engine;
            TemplateKey = key;
            TemplateType = templateType;
        }

        public RazorEngine<TTemplate> Engine { get; private set; }

        public ITemplateKey TemplateKey { get; private set; }

        public Type TemplateType { get; private set; }

        public override string Message
        {
            get
            {
                if (HasCustomMessage)
                    return base.Message;

                var innerMessage = InnerException == null
                    ? Resources.UnspecifiedError
                    : InnerException.Message
                ;

                return string.Format(Resources.TemplateInstanceError, innerMessage);
            }
        }
    }

    public class RazorTemplateCompileException<TTemplate> : RazorException
        where TTemplate : RazorTemplateBase, new()
    {

        public RazorTemplateCompileException(RazorEngine<TTemplate> engine, string message, Exception innerException)
            : base(message, innerException)
        {
            Engine = engine;
        }

        public RazorEngine<TTemplate> Engine { get; private set; }

        public override string Message
        {
            get
            {
                if (HasCustomMessage)
                    return base.Message;

                var innerMessage = InnerException == null
                    ? Resources.UnspecifiedError
                    : InnerException.Message
                ;

                return string.Format(Resources.TemplateCompileError, innerMessage);
            }
        }
    }

    public class RazorDebuggableTemplateCompileException<TTemplate> : RazorTemplateCompileException<TTemplate>
        where TTemplate : RazorTemplateBase, new()
    {
        public RazorDebuggableTemplateCompileException(RazorEngine<TTemplate> engine, IReadOnlyList<RazorTemplateCompileInfo> entries, string message, Exception innerException)
            : base(engine, message, innerException)
        {
            Entries = entries;
        }

        public IReadOnlyList<RazorTemplateCompileInfo> Entries { get; private set; }

        public override string Message
        {
            get
            {
                if (HasCustomMessage)
                    return base.Message;

                StringBuilder message = new StringBuilder();

                message
                    .AppendLine(string.Format(Resources.TemplateCompileError, string.Empty))
                    .AppendLine()
                ;

                foreach (var entry in Entries)
                {
                    if (entry.Errors.Count > 0)
                    {
                        message
                            .Append(entry.FileName)
                            .AppendLine(":")
                        ;

                        foreach (var error in entry.Errors)
                        {
                            message
                                .Append("  - ")
                                .Append(error.IsWarning ? "warning: " : "error: ")
                                .Append(error.ErrorNumber)
                                .Append(" - ")
                                .AppendFormat("({0:D}, {1:D})", error.Line, error.Column)
                                .Append(" ")
                                .Append(error.ErrorText)
                                .AppendLine()
                            ;
                        }
                    }
                }

                SetMessage(message.ToString());
                return base.Message;
            }
        }
    }

    #endregion

    public class RazorTemplateCompileInfo
    {
        public string FileName { get; set; }
        public string SourceCode { get; set; }
        public System.CodeDom.CodeCompileUnit CodeDom { get; set; }
        public string GeneratedCode { get; set; }
        public IReadOnlyList<System.CodeDom.Compiler.CompilerError> Errors { get; set; }
    }

    internal static class Resources
    {
        /// <summary>
        /// Template file doesn't exist: {0}
        /// </summary>
        public const string TemplateFileNotFound = @"Template file doesn't exist: {0}";

        /// <summary>
        /// Error reading template file: {0}
        /// </summary>
        public const string TemplateFileReadingError = @"Template file reading error: {0}";

        /// <summary>
        /// Template compiling error: {0}
        /// </summary>
        public const string TemplateCompileError = @"Template compiling error: {0}";

        /// <summary>
        /// Template creating error: {0}
        /// </summary>
        public const string TemplateInstanceError = @"Template creating error: {0}";

        /// <summary>
        /// Unable to create template.
        /// </summary>
        public const string UnableToCreateTemplate = @"Unable to create template.";

        /// <summary>
        /// Template execution error: {0}
        /// </summary>
        public const string TemplateExecutionError = @"Template execution error: {0}";

        /// <summary>
        /// Error reading template.
        /// </summary>
        public static string TemplateReadingError = @"Template reading error.";

        /// <summary>
        /// Compiled template not found.
        /// </summary>
        public const string CompiledTemplateNotFound = @"Compiled template not found.";

        /// <summary>
        /// Unspecified error.
        /// </summary>
        public const string UnspecifiedError = @"Unspecified error.";
    }
}
