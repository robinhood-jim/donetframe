using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.Common.Pdf
{
    public class ITextPdfFormGenerator
    {
        /// <summary>
        /// Generate Pdf Document using Template and Parameter Dictionary
        /// </summary>
        /// <param name="templateStream">template inputStream</param>
        /// <param name="outputStream">outputStream </param>
        /// <param name="paramMap"> Parameters </param>
        /// <param name="fontSize">font Size </param>
        /// <param name="fontProgram"></param>
        /// <param name="encoding"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static void GenerateWithTemplate(Stream templateStream, Stream outputStream, Dictionary<string, string> paramMap, int fontSize = 0, string? fontProgram = null, string encoding = "")
        {
            using PdfDocument document = new(new PdfReader(templateStream), new PdfWriter(outputStream));
            //Read Pdf Form 
            PdfAcroForm form = PdfAcroForm.GetAcroForm(document, true);

            PdfFont? font = null!;
            // selected Font
            if (!string.IsNullOrWhiteSpace(fontProgram) || !string.IsNullOrWhiteSpace(encoding))
            {
                string defaultProgram = fontProgram ?? "STSongStd-Light";
                font = PdfFontFactory.CreateFont(defaultProgram, encoding, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            }
            //Get Form Fields
            IDictionary<string, PdfFormField> formFields = form.GetAllFormFields();
            if (!formFields.IsNullOrEmpty())
            {
                IEnumerator<KeyValuePair<string, PdfFormField>> enumerator = formFields.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, PdfFormField> keyValuePair = enumerator.Current;
                    if (paramMap.TryGetValue(keyValuePair.Key, out string? paramValue) && !string.IsNullOrWhiteSpace(paramValue))
                    {
                        if (font != null)
                        {
                            keyValuePair.Value.SetFont(font);
                        }
                        keyValuePair.Value.SetValue(paramValue);
                        if (fontSize > 0)
                        {
                            keyValuePair.Value.SetFontSize(fontSize);
                        }
                        keyValuePair.Value.SetReadOnly(true);
                    }
                    else
                    {
                        Trace.WriteLine($"param {keyValuePair.Key} does not contain values");
                    }
                }
                form.FlattenFields();
            }
            else
            {
                throw new NotSupportedException(" template does not contain any Fields");
            }
        }
    }
}
