![logo](https://raw.githubusercontent.com/sh0ou/AssetReferLinker/main/Packages/jp.sh0uroom.assetlinker/UI/logo.png)
# AssetLinker
- [日本語](https://github.com/sh0ou/AssetReferLinker/blob/main/README-JP.md)
- [English](https://github.com/sh0ou/AssetReferLinker/blob/main/README.md)

AssetLinker is a tool for storing dependencies such as download URLs, license URLs, and file paths as JSON files for easy reference.

This is useful when collaborating on projects that use paid assets or assets that cannot be redistributed.

# Change Languages
![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/abba2866-65a6-4ff0-8950-6f4126034a62)

Change to Window>AssetLinker>Settings or open from "Change Language" in the Welcome window.<br/>
At this time, English and Japanese are supported.

# How to use
## Create Link

From Project view, select and right-click on the root folder of the asset you wish to associate.

For example, if you have a folder such as "Assets/AssetLinker/UI," right click with "Assets/AssetLinker" open.

![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/5a7d74d2-872a-4abd-bb52-e04066cdcb89)

When the right-click menu opens, select "AssetLinker".

![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/6a24e42c-5f18-4cd7-ae34-e1d9d154c5cb)

> [!NOTE]
> Make sure the asset folder you have selected is correct.<br/>
> The folder name becomes the .astlnk file name as it is.

Enter the URL of where to download the asset (or sales page).
At this time, the following URL detection is supported.
- AssetStore
- Github
- BOOTH
- Gumroad
- VketStore

Of course, URLs not listed in these can also be linked.

![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/b3d3c547-c8e9-4940-97e0-2b966beb5b3b)


After entering the download URL, a screen for entering detailed information will appear.<br/>
Here you can select the URL of the license, whether the asset is free or not, and the files to be tracked.

> [!WARNING]
> **Do not change the assets to be tracked from their initial state!**
> 
> AssetLinker is a tool that verifies only whether an asset is there or not.<br/>
> It is assumed that the state of the asset has not been modified in any way since it was imported.
> 
> If you want to save and share the state of the assets, please prefab the GameObjects that use the assets and store them in a separate folder, for example.

Click the "Link!" button to create the link.<br/>
The created .astlnk file will be saved in /\[ProjectName]/AssetLinker/.

## Validate Link
![image](https://github.com/sh0ou/AssetReferLinker/assets/47475540/984c415d-fe96-4e63-a677-b210935ff1ba)

By default, if the astlnk file exists, the asset is automatically verified for its existence.

If a specific asset file or folder does not exist, an asset confirmation window will automatically appear.<br/>
Red text in the file path indicates assets that do not exist in the project.

> [!NOTE]
> The window is automatically displayed when the project is started.
> This can be set to not appear automatically the next time from the bottom right of the confirmation window or from the settings screen.

The following buttons exist in each asset menu.<br/>
Any window will be executed only if "Yes" is selected in the confirmation window.

- Download
  - Open the download page or the sales page.
- License
  - Open the page containing the Terms of Use or License.
- UnLink
  - Unlink to AssetLinker. The corresponding .astlnk file will be deleted.
 
#  Report & Request
We accept them via the Github issue / PR feature.<br/>

[Bug Report](https://github.com/sh0ou/AssetReferLinker/issues/new?assignees=&labels=T%3A+Bug&projects=&template=bug_report.md&title=)

[Request](https://github.com/sh0ou/AssetReferLinker/issues/new?assignees=&labels=P3%3A+Medium%2C+T%3A+Enhancement&projects=&template=feature_request.md&title=)

(DeepL Translate. We are looking for translations.)
