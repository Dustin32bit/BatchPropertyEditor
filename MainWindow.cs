using System;
using System.IO;
using System.Collections.Generic;
using Gtk;

public partial class MainWindow: Gtk.Window
{

	ListStore fileListStore = new ListStore(typeof(string));

	string operationLog;
	string lastPath;
	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();
		CellRendererText fileListRenderer = new CellRendererText();
		TreeViewColumn fileNameColumn = new TreeViewColumn("Filename", fileListRenderer);


		fileList.AppendColumn(fileNameColumn);
		fileList.Model = fileListStore;
		fileList.Selection.Mode = SelectionMode.Multiple;
		fileNameColumn.AddAttribute(fileListRenderer, "text", 0);

		propertyInput.WrapWidth = 3;
		//propertyInput.SetAttributes(
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected void AddFile (object sender, EventArgs e)
	{
		FileChooserDialog dialog = new FileChooserDialog("Select File", this, FileChooserAction.Open, "Add", ResponseType.Accept, "Cancel", ResponseType.Cancel);
		if(lastPath != string.Empty)
		{
			dialog.SetFilename(lastPath);
		}
		dialog.SelectMultiple = true;



		if(dialog.Run() == (int)ResponseType.Accept)
		{
			

			for(int i = 0; i < dialog.Filenames.Length; i++)
			{
				fileListStore.AppendValues(dialog.Filenames[i]);
			}
		}
		lastPath = dialog.Filename;
		dialog.Destroy();
			
	} 


	protected int FindProperty(string propertyName, string[] file, List<int> properties = null)
	{
		int count = 0;
		bool saveLineNumbers = false;

		if(propertyName == String.Empty )
		{
			return 0;
		}
		//string[] file = File.ReadAllLines(filename);

		if(properties != null)
		{
			saveLineNumbers = true;
		}
		for(int i = 0; i < file.Length; i++)
		{
			if(file[i].IndexOf(propertyName) == 0)
			{
				if(saveLineNumbers)
				{
					properties.Add(i);
				}
				count++;
			}

		}
		return count;
	}
	protected void SearchForProperty (object sender, EventArgs e)
	{
		TreeIter iter;
		string filename;
		int totalCount = 0;
		if(fileListStore.GetIterFirst(out iter))
		{
			
			do
			{
				filename = fileListStore.GetValue(iter, 0) as String;
				totalCount += FindProperty(propertyInput.ActiveText, File.ReadAllLines(filename));

			}while(fileListStore.IterNext(ref iter));
			searchReportLabel.Text = "Found " + totalCount + " instances.";
		}
	}

	protected void EditProperty (string propertyName, string filename, float value)
	{
		List<int> targetLines = new List<int>();
		char delimiter = '=';
		string[] file = File.ReadAllLines(filename);
		string[] splitLine;
		float newValue = 0;

		FindProperty(propertyName, file, targetLines);

		if(delimiterCheckBox.Active)
		{

			delimiter = delimiterEntry.Text[0];
		}
		for(int i = 0; i < targetLines.Count; i++)
		{
			splitLine = file[targetLines[i]].Split(new[]{delimiter}, 2);
			splitLine[1] = splitLine[1].Trim(new[]{'\t', ' '});
			for(int k = 0; k < splitLine[1].Length; k++)
			{
				//operationLog += "Checking char: " + splitLine[1][k] + '\n';
				if(!char.IsDigit(splitLine[1][k]) && splitLine[1][k] != '.')
				{
					//operationLog += "Trimming " + splitLine[1] + " into ";
					splitLine[1] = splitLine[1].Remove(k);
					//operationLog += splitLine[1] + "\n";

				}
			}
			//splitLine[1] = new string(Array.FindAll(splitLine[1].ToCharArray(), (c => char.IsDigit(c))));

			if(float.TryParse(splitLine[1], out newValue))
			{
				splitLine[0] = splitLine[0].Trim(new[]{'\t', ' '});
				operationLog += "Changed " + file[targetLines[i]] + " into ";
				if(multiplyRadioButton.Active)
				{
					newValue = float.Parse(splitLine[1]) * value;
				}
				else
				{
					newValue = float.Parse(splitLine[1]) + value;
				}

				if(integerModeCheck.Active)
				{
					newValue = (int)newValue;
				}


				splitLine[1] = newValue.ToString();
				file[targetLines[i]] = String.Join("     =     ", splitLine);

				operationLog += file[targetLines[i]] + " in file " + filename  + '\n';
			}
		}
		File.WriteAllLines(filename, file);

	}

	protected void RemoveSelected (object sender, EventArgs e)
	{

		TreeIter iter;
		TreePath[] paths = fileList.Selection.GetSelectedRows();
		for(int i = 0; i < paths.Length; i++)
		{
			fileListStore.GetIter(out iter, paths[0]);
			fileListStore.Remove(ref iter);
		}

	}

	protected void CommitChanges (object sender, EventArgs e)
	{
		TreeIter iter;

		MessageDialog successDialog = new MessageDialog(this, DialogFlags.DestroyWithParent, MessageType.Info, ButtonsType.Ok, "Success!");
		operationLog = "";

		if(fileListStore.GetIterFirst(out iter))
		{

			do
			{
				EditProperty(propertyInput.ActiveText, fileListStore.GetValue(iter, 0) as String, (float)numInput.Value);
			}while(fileListStore.IterNext(ref iter));
		}

		logWindow.Buffer.Text = operationLog;
		successDialog.Run();
		successDialog.Destroy();
		//editReportLabel.Text = "Changed " + totalCount + " instances.";
	}

	protected void delimiterToggled (object sender, EventArgs e)
	{
		delimiterEntry.Sensitive = delimiterCheckBox.Active;
	}
}
