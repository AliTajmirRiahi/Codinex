/**
 * sampleChangeset.js
 * Fixture used only for standalone browser testing of the Code Changes review
 * view (change-review-view.html opened directly, outside WebView2/Visual Studio).
 * Not referenced by production code paths.
 */
export const sampleChangeset = {
    id: 'test-changeset-1',
    summary:
        '- Enhanced IErrorHandler interface with context support and cancellation\n' +
        '- Added CanHandle method to check if exception can be handled\n' +
        '- Implemented updated interface in ErrorHandler service\n' +
        '- Added ErrorLogger for improved logging\n' +
        '- Added unit tests for new functionality',
    files: [
        {
            filePath: 'src/Codify.Core/Interfaces/IErrorHandler.cs',
            operation: 'EditFile',
            originalText:
                'using System;\n' +
                'using System.Threading.Tasks;\n' +
                '\n' +
                'namespace Codify.Core.Interfaces\n' +
                '{\n' +
                '    public interface IErrorHandler\n' +
                '    {\n' +
                '        void Handle(Exception exception);\n' +
                '        Task HandleAsync(Exception exception);\n' +
                '    }\n' +
                '}\n',
            modifiedText:
                'using System;\n' +
                'using System.Threading.Tasks;\n' +
                '\n' +
                'namespace Codify.Core.Interfaces\n' +
                '{\n' +
                '    public interface IErrorHandler\n' +
                '    {\n' +
                '        void Handle(Exception exception, string? context = null);\n' +
                '        Task HandleAsync(Exception exception, string? context = null);\n' +
                '        bool CanHandle(Exception exception);\n' +
                '    }\n' +
                '}\n'
        },
        {
            filePath: 'src/Codify.Core/Services/ErrorHandler.cs',
            operation: 'EditFile',
            originalText:
                'public class ErrorHandler : IErrorHandler\n' +
                '{\n' +
                '    public void Handle(Exception exception)\n' +
                '    {\n' +
                '        Console.WriteLine(exception.Message);\n' +
                '    }\n' +
                '}\n',
            modifiedText:
                'public class ErrorHandler : IErrorHandler\n' +
                '{\n' +
                '    private readonly IErrorLogger _logger;\n' +
                '\n' +
                '    public ErrorHandler(IErrorLogger logger)\n' +
                '    {\n' +
                '        _logger = logger;\n' +
                '    }\n' +
                '\n' +
                '    public void Handle(Exception exception, string? context = null)\n' +
                '    {\n' +
                '        _logger.Log(exception, context);\n' +
                '    }\n' +
                '\n' +
                '    public bool CanHandle(Exception exception) => exception != null;\n' +
                '}\n',
            previewWarning:
                'Could not preview edit #2: SearchNotFound (the expected text was not found). ' +
                'The diff shown reflects only the edits applied before this point.'
        },
        {
            filePath: 'src/Codify.Core/Extensions/ServiceCollectionExtensions.cs',
            operation: 'CreateFile',
            originalText: '',
            modifiedText:
                'public static class ServiceCollectionExtensions\n' +
                '{\n' +
                '    public static IServiceCollection AddErrorHandling(this IServiceCollection services)\n' +
                '    {\n' +
                '        services.AddSingleton<IErrorHandler, ErrorHandler>();\n' +
                '        services.AddSingleton<IErrorLogger, ErrorLogger>();\n' +
                '        return services;\n' +
                '    }\n' +
                '}\n'
        },
        {
            filePath: 'src/Codify.Infrastructure/Logging/ErrorLogger.cs',
            operation: 'CreateFile',
            originalText: '',
            modifiedText:
                'public sealed class ErrorLogger : IErrorLogger\n' +
                '{\n' +
                '    public void Log(Exception exception, string? context)\n' +
                '    {\n' +
                '        Trace.WriteLine($"[{context}] {exception}");\n' +
                '    }\n' +
                '}\n'
        },
        {
            filePath: 'tests/Codify.Core.Tests/ErrorHandlerTests.cs',
            operation: 'DeleteFile',
            originalText:
                '[TestFixture]\n' +
                'public class ErrorHandlerTests\n' +
                '{\n' +
                '    [Test]\n' +
                '    public void Handle_LogsMessage()\n' +
                '    {\n' +
                '        // superseded by ErrorHandlerServiceTests\n' +
                '    }\n' +
                '}\n',
            modifiedText: ''
        }
    ]
};
