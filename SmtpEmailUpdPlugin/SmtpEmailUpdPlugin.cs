﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using FabSoftUpd.Wizard.Workflows_v1.Properties;
using System.IO;
using FabSoftUpd;
using FabSoftUpd.Wizard;
using System.Net;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace SmtpEmailUpdPlugin
{
    public class SampleSmtpEmailUpdPlugin : FabSoftUpd.Wizard.Workflows_v1.BaseDeliveryWorkflow
    {
        public SampleSmtpEmailUpdPlugin()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            BitmapImage bitmap = null;

            Stream img = GetEmbeddedResource("Workflow_Icon.png");

            if (img != null)
            {
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = img;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            _CustomIcon = bitmap;
        }
        private static BitmapSource _CustomIcon = null;
        public override BitmapSource CustomIcon
        {
            get
            {
                return _CustomIcon;
            }
        }
        private static Stream GetEmbeddedResource(string resourceName)
        {
            return GetEmbeddedResource(resourceName, Assembly.GetExecutingAssembly());
        }

        private static Stream GetEmbeddedResource(string resourceName, Assembly assembly)
        {
            resourceName = FormatResourceName(assembly, resourceName);
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                MemoryStream tmpStream = null;
                if (resourceStream != null)
                {
                    tmpStream = new MemoryStream();
                    resourceStream.CopyTo(tmpStream);
                    resourceStream.Seek(0, SeekOrigin.Begin);
                }
                return tmpStream;
            }
        }

        private static string FormatResourceName(Assembly assembly, string resourceName)
        {
            return assembly.GetName().Name + "." + resourceName.Replace(" ", "_")
                                                               .Replace("\\", ".")
                                                               .Replace("/", ".");
        }

        private const int LATEST_VERSION = 1;
        public override int WorkflowVersion { get; set; } = LATEST_VERSION; // Don't hard code the get, needed for upgrades

        public override DocumentWorkflow UpgradeWorkflow()
        {
            DocumentWorkflow upgradedWorkflow = base.UpgradeWorkflow();
            if (this.WorkflowVersion == LATEST_VERSION)
            {
                //Upgrade Workflow
            }
            return upgradedWorkflow;
        }


        public override bool CanAddFields
        {
            get
            {
                return false;
            }
        }

        public override bool CanTestOutput
        {
            get
            {
                return true;
            }
        }

        public override string DisplayName
        {
            get
            {
                return "Sample SMTP Email";
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void AttachDebugger() // Attach debugger if none is already attached and it is compiled for DEBUG.
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                System.Diagnostics.Debugger.Launch();
            }
        }

        public override List<BaseProperty> GetDefaultWorkflowProperties(string jobName, string printerName)
        {
            AttachDebugger();

            List<BaseProperty> defaultProperties = new List<BaseProperty>();

            defaultProperties.Add(new OutputImageTypeProperty("OutputImageType", "Output File Type") { UserGenerated = false });

            defaultProperties.Add(new PrinterOutputProperty("Printer", "Printer")
            {
                UserGenerated = false,
                JobName = jobName,
                PrintEnabled = false,
                IsConfigured = true
            });

            defaultProperties.Add(new StaticTextProperty("SMTP_Server", "SMTP Server Name / Address")
            {
                UserGenerated = false
            });

            defaultProperties.Add(new StaticNumberProperty("SMTP_Port", "SMTP TCP Port")
            {
                UserGenerated = false,
                Value = "587",
                IsConfigured = true
            });

            defaultProperties.Add(new StaticYesNoProperty("SMTP_SSL", "SMTP Enable TLS")
            {
                UserGenerated = false,
                Value = "YES",
                IsConfigured = true
            });

            defaultProperties.Add(new StaticTextProperty("SMTP_Username", "SMTP Username")
            {
                UserGenerated = false,
                IsRequired = false,
                IsConfigured = true,
                Value = ""
            });

            defaultProperties.Add(new StaticPasswordProperty("SMTP_Password", "SMTP Server Password")
            {
                UserGenerated = false,
                IsRequired = false,
                IsConfigured = true,
                Value = ""
            });

            defaultProperties.Add(new AnyInputSourceProperty("Sender_EmailAddress", "Sender Email Address")
            {
                UserGenerated = false,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Sender_Name", "Sender Name")
            {
                UserGenerated = false,
                IsRequired = false,
                IsConfigured = true,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Recipient_EmailAddress", "Recipient Email Address")
            {
                UserGenerated = false,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Recipient_Name", "Recipient Name")
            {
                UserGenerated = false,
                IsRequired = false,
                IsConfigured = true,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Recipient_CCEmailAddress", "Recipient CC Email Address")
            {
                UserGenerated = false,
                IsRequired = false,
                IsConfigured = true,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Recipient_BCCEmailAddress", "Recipient BCC Email Address")
            {
                UserGenerated = false,
                IsRequired = false,
                IsConfigured = true,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Message_Subject", "Message Subject")
            {
                UserGenerated = false,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Message_Body", "Message Body")
            {
                UserGenerated = false,
                CanHaveUserInteraction = true
            });

            defaultProperties.Add(new AnyInputSourceProperty("Message_Attachment_Name", "Message Attachment Name")
            {
                UserGenerated = false,
                CanHaveUserInteraction = true
            });

            return defaultProperties;
        }

        public override SubmissionStatus ServiceSubmit(string jobName, string jobPath, string fclInfo, Dictionary<string, string> driverSettings, Logger externalHandler, Stream xpsStream, int pageIndexStart, int pageIndexEnd, List<PageDimensions> pageDimensions)
        {

            AttachDebugger();

            SubmissionStatus status = new SubmissionStatus();
            status.Result = false;
            try
            {
                string recipientEmail = GetPropertyString("Recipient_EmailAddress");
                string recipientName = GetPropertyString("Recipient_Name");
                MailAddress emailRecipient = new MailAddress(recipientEmail, recipientName);
                status.Destination = emailRecipient.ToString();
                using (SmtpClient smtpConn = new SmtpClient())
                {
                    using (MailMessage newMessage = new MailMessage())
                    {

                        smtpConn.Host = GetPropertyString("SMTP_Server");
                        smtpConn.EnableSsl = (GetPropertyString("SMTP_SSL") == "NO" ? false : true);

                        string username = GetPropertyString("SMTP_Username");
                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            smtpConn.Credentials = new System.Net.NetworkCredential(username, GetPropertyString("SMTP_Password"));
                        }

                        int? portNumber = GetPropertyInteger("SMTP_Port");
                        if (!portNumber.HasValue) { portNumber = 587; }
                        smtpConn.Port = portNumber.Value;

                        newMessage.From = new MailAddress(GetPropertyString("Sender_EmailAddress"), GetPropertyString("Sender_Name"));

                        newMessage.To.Add(emailRecipient);

                        string ccAddress = GetPropertyString("Recipient_CCEmailAddress");
                        if (!string.IsNullOrWhiteSpace(ccAddress))
                        {
                            newMessage.CC.Add(new MailAddress(ccAddress));
                        }

                        string bccAddress = GetPropertyString("Recipient_BCCEmailAddress");
                        if (!string.IsNullOrWhiteSpace(bccAddress))
                        {
                            newMessage.Bcc.Add(new MailAddress(bccAddress));
                        }

                        newMessage.Subject = GetPropertyString("Message_Subject");
                        newMessage.Body = GetPropertyString("Message_Body");

                        string attachmentName = GetPropertyString("Message_Attachment_Name");
                        if (string.IsNullOrWhiteSpace(attachmentName)) { attachmentName = "Document"; }

                        OutputImageTypeProperty outputImageType = null;
                        if (GetProperty("OutputImageType", out outputImageType, false))
                        {
                            if (outputImageType != null)
                            {

                                DocumentRenderer renderingConverter = GetRenderer(outputImageType);

                                if (renderingConverter != null)
                                {
                                    TempFileStream outputStream = null;
                                    try
                                    {
                                        try
                                        {
                                            string tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Temp\");
                                            Directory.CreateDirectory(tempFolder);
                                            outputStream = new TempFileStream(tempFolder);
                                        }
                                        catch (Exception)
                                        {
                                            //Ignore - attempt another temp location (UAC may block UI from accessing Temp folder.
                                        }

                                        if (outputStream == null)
                                        {
                                            string tempFolder = Path.Combine(Path.GetTempPath(), @"FS_UPD_v4\Email_SMTP\");
                                            Directory.CreateDirectory(tempFolder);
                                            outputStream = new TempFileStream(tempFolder);
                                        }

                                        renderingConverter.RenderXpsToOutput(xpsStream, outputStream, pageIndexStart, pageIndexEnd, externalHandler);

                                        outputStream.Seek(0, SeekOrigin.Begin);
                                        attachmentName += renderingConverter.FileExtension;
                                        Attachment mailAttachment = new Attachment(outputStream, attachmentName);

                                        newMessage.Attachments.Add(mailAttachment);


                                        smtpConn.Send(newMessage);
                                    }
                                    finally
                                    {
                                        if (outputStream != null)
                                        {
                                            outputStream.Dispose();
                                            outputStream = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                status.Result = true;
                status.Message = "Successfully sent the email to " + emailRecipient.ToString();
                status.SeverityLevel = StatusResults.SeverityLevels.OK;
                status.StatusCode = 0;
            }
            catch (Exception ex)
            {
                externalHandler.LogMessage("SMTP Email Error: " + ex.ToString());
                status.Message = "Email Error: " + ex.Message.ToString();
                status.LogDetails = "Email Error: " + ex.ToString();
                status.SeverityLevel = StatusResults.SeverityLevels.High;
                status.StatusCode = 1;
                status.IsUserCorrectable = false;
                status.NotifyUser = true;
            }

            externalHandler.LogMessage("SMTP Email Result: " + status.Result.ToString());

            return status;
        }

        private string GetPropertyString(string propertyName)
        {
            string foundValue = "";

            BaseProperty foundProperty;
            if (GetProperty(propertyName, out foundProperty, false))
            {
                foundValue = foundProperty.GetResult();
            }

            return foundValue;
        }

        private int? GetPropertyInteger(string propertyName, int? defaultValue = null)
        {
            int? foundValue = defaultValue;

            BaseProperty foundProperty;
            if (GetProperty(propertyName, out foundProperty, false))
            {
                string tmps = foundProperty.GetResult();
                int tmpv;
                if (int.TryParse(tmps, out tmpv))
                {
                    foundValue = tmpv;
                }
            }

            return foundValue;
        }
    }
}