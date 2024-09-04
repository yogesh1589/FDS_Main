using FDS;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public class UINavigationManager
{
    private frmFDSMain mainWindow; // Assume MainWindow contains all the UI controls

    public UINavigationManager(FDSMain mainWindow)
    {
        this.mainWindow = mainWindow;
    }

    public void LoadMenu(Screens screen)
    {
        try
        {
            HideAllControls();
            SetCommonProperties();

            switch (screen)
            {
                case Screens.GetOTP:
                    mainWindow.GetOTP.Visibility = Visibility.Visible;
                    break;
                case Screens.GetStart:
                    ConfigureGetStartScreen();
                    break;
                case Screens.AuthenticationMethods2:
                    ConfigureAuthMethods2Screen();
                    break;
                // Add more cases for each screen...
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            if (mainWindow.showMessageBoxes == true)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void HideAllControls()
    {
        // Set Visibility.Hidden for all controls initially
        mainWindow.cntGetStart.Visibility = Visibility.Hidden;
        mainWindow.cntQRCode.Visibility = Visibility.Hidden;
        mainWindow.cntServiceSetting.Visibility = Visibility.Hidden;
        // Continue hiding all necessary controls...
    }

    private void SetCommonProperties()
    {
        // Apply common settings such as background colors or headers here if any
        mainWindow.Header.Background = new SolidColorBrush(Color.FromRgb(30, 47, 96));
    }

    private void ConfigureGetStartScreen()
    {
        // Configure visibility and other properties specific to the GetStart screen
        mainWindow.cntGetStart.Visibility = Visibility.Visible;
        mainWindow.Header.Visibility = Visibility.Visible;
        // Additional settings...
    }

    private void ConfigureAuthMethods2Screen()
    {
        // Configure visibility and other properties specific to the AuthenticationMethods2 screen
        mainWindow.AuthenticationMethods2.Visibility = Visibility.Visible;
        // Additional settings...
    }
}

public enum Screens
{
    GetOTP,
    GetStart,
    AuthenticationMethods2,
    // Add other screen types...
}
