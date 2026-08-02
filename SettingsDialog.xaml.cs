using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ViscaIP;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsDialog : ContentDialog
{

	public SettingsDialog()
	{
		InitializeComponent();

		this.Height = 400;
		this.Title = "Settings";
		this.CloseButtonText = "Done";
		this.Loaded += SettingsDialog_Loaded;
	}

	private void SettingsDialog_Loaded(object sender, RoutedEventArgs e)
	{
		for (uint i = 1; i < 8; i++) {
			string[] camAry = Config.GetCamera((int)i);
			if (camAry.Length > 1) {
				cameraCombo.Items.Add(i.ToString());
			}
		}

		for (int i = 0; i < Config.CameraModelStrs.Length; i++) { 
			modelCombo.Items.Add(Config.CameraModelStrs[i]);	
		}
	
		miniCheck.IsChecked = Config.Mini;
		commsCheck.IsChecked = Config.Debug;
		logCheck.IsChecked = Config.Log;
	}

	private void cameraCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		string? num = cameraCombo.SelectedItem as string;
		if (num != null) {
			int cameraNum = int.Parse(num);
			string[] camAry = Config.GetCamera(cameraNum);
			if (camAry.Length > 1) {
				ipAddressTxt.Text = camAry[0];
				portTxt.Text = camAry[1];
				nameTxt.Text = camAry[2];
				for (int i = 0; i < modelCombo.Items.Count; i++) {
					if (modelCombo.Items[i] is ComboBoxItem item) {
						if (item.Content.ToString() == camAry[3]) {
							modelCombo.SelectedIndex = i;
							break;
						}
					}
				}
			} else {
				ipAddressTxt.Text = "";
				portTxt.Text = "";
				nameTxt.Text = "";
			}
		}
	}
	

	private void AddClick(object sender, RoutedEventArgs e) {
		int count = cameraCombo.Items.Count;
		if (count < 7) {
			for (uint i = 1; i < 8; i++) {
				string[] camAry = Config.GetCamera((int)i);
				if (camAry.Length < 2) {
					string cameraNum = i.ToString();
					cameraCombo.Items.Add(cameraNum);
					cameraCombo.UpdateLayout();
					break;
				}
			}
		}
	}

	private void SaveClick(object sender, RoutedEventArgs e) {
		string? num = cameraCombo.SelectedItem as string;
		if (num != null) {
			int cameraNum = int.Parse(num);	
			string[] camAry = Config.GetCamera(cameraNum);
			if (camAry.Length < 2) {
				camAry = new string[10];
			}

			camAry[0] = ipAddressTxt.Text;
			camAry[1] = portTxt.Text;
			camAry[2] = nameTxt.Text;
			if (modelCombo.SelectedItem != null) {
				camAry[3] = modelCombo.SelectedItem.ToString();
			}
			Config.SetCamera(cameraNum, camAry);
		}
	}

	private void DeleteClick(object sender, RoutedEventArgs e) {
		string? num = cameraCombo.SelectedItem as string;
		if (num != null) {
			int cameraNum = int.Parse(num);
			string[] camAry = new string[1];
			Config.SetCamera(cameraNum, camAry);
		}
	}

	private void miniCheck_Checked(object sender, RoutedEventArgs e) {
		Config.Mini = true;
	}

	private void miniCheck_Unchecked(object sender, RoutedEventArgs e) {
		Config.Mini = false;
	}

	private void commsCheck_Checked(object sender, RoutedEventArgs e) {
		Config.Debug = true;
	}

	private void commsCheck_Unchecked(object sender, RoutedEventArgs e) {
		Config.Debug = false;
	}

	private void logCheck_Checked(object sender, RoutedEventArgs e) {
		Config.Log = true;
	}

	private void logCheck_Unchecked(object sender, RoutedEventArgs e) {
		Config.Log = false;
	}
}
