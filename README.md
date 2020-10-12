### Warning
This script was created in under 3 hours and thus consider it not production ready missing many optimizations and error handling.

### Reason
This script was created to rename datasource output from CHILI publisher using the naming pattern found in CHILI publisher products.

This was developed as a workaround but could potentially be used with older versions of CHILI publisher that do not support pattern naming of datasource output.

Finally, this could be forked and reworked to provide advance pattern naming that cannot be done in CHILI publisher.

### Limitations
Currently this script only supports $var_XXX% of the naming pattern introduced in CHILI publisher 5.7.

### Build
You can build this application by running
```shell
dotnet publish -r {RID} -p:PublishSingleFile=true --self-contained true -o {location}
```

After build, you should create an unpack and config folder then add the pattern.xml. Running the application will also create these folders/files for you.

------------


## Use
### Getting Output Package
This script will rename datasource output from CHILI publisher. Therefore you will need to provide a zip package of this output.

The following are requirements of your PDF Export Settings to make a compatible output. For Image Conversion Profiles, if you are doing an image ouput instead of a PDF output, the settings do not matter. However, you must have the following in your PDF Export Settings whether you are doing PDF output or image output.

The below settings will be found in the DataSource tab of your PDF Export Settings.

- PDF Naming Pattern - it must be empty (this may not exist in older versions)
- Max Rows - set to -1
- Rows / PDF - set to 1
- Create seperate background PDF - unchecked
- Include backgroubd layers in variable PDF - unchecked
- Include creation log - unchecked
- Export datasource - **checked**

If any of the above settings do not match, the script will not be able to process the output zip.

### Renaming Output
Once you have the output zip, place the zip file in the unpack folder of the application directory. Then in the config folder you will find a pattern.xml file.

#### Pattern.xml Configuration
You must add a `<pattern></pattern>` element with a name attribute that is equal to the output zip.

The inner text value of the pattern tag should match the PDF Name Pattern that would be used in CHILI publicher 5.7 or above.

Currently only %var_XXX% is supported. 

For example, if your output zip is called Example.zip and the document outputted has two variables you want in the file name:
- First Name
- Last Name

You would then make configer the pattern.xml as the following:
```xml
<patterns>
    <pattern name="Example">example_%var_First NameY%_%Last Name%</pattern>
</patterns>
```

If you want to process more than one output, it is very simple. Just place the zip packages in the unpack folder and add more `<pattern></pattern>` elements to the pattern.xml.

For example, if you have to outputs to process: Example.zip and Diff Example.zip your pattern.xml would look like:
```xml
<patterns>
    <pattern name="Example">example_%var_First NameY%_%Last Name%</pattern>
	<pattern name="Diff Example">example_%var_Another Variable%_%CASE SENSITIVE%</pattern>
</patterns>
```

**Note!!!** Variables are case sensitive to the name in the datasource column name. Please make sure the name used in your pattern matching is the exact match in the datasource column.
&nbsp;
&nbsp;
Once complete, your output will be found in the output folder.