# Browser-Reviewer
Browser Reviewer is a portable forensic tool for analyzing user activity in Firefox and Chrome-based browsers. It extracts and displays browsing history, downloads, bookmarks, and autofill data. The tool allows analysts to tag, comment, and export detailed reports in PDF or HTML format.

It requires no installation and can be executed directly from a USB drive or over a network share — ideal for forensic workflows with minimal footprint on the target system.

![imagen](https://github.com/user-attachments/assets/3395cf20-1b7f-472b-8dee-7622d6876262)


## 🚀 Getting Started

### 🔍 Extracting Browser Artifacts

To begin analyzing browser activity:

1. Go to **File > New**
2. Choose a **file name** for the new project (a SQLite database will be created)
3. Click **Scan Web Activity**

Browser Reviewer will scan the selected path for supported browser artifacts from **Firefox** and **Chrome/Chromium-based browsers**, including:

- 🕓 Browsing history  
- ⬇️ Download history  
- 🔖 Bookmarks  
- 🧠 Autofill form data  

Once processed, the data will appear in the main table, where you can filter, search, tag, and comment on individual entries.

- Use the **Label Manager** to create and assign custom tags to records.

![imagen](https://github.com/user-attachments/assets/fd3b890a-2476-4573-8547-fb9d6ace97d1)


- Set the **UTC offset** at the top of the interface to adjust all timestamps to the correct time zone.


![imagen](https://github.com/user-attachments/assets/ca1c4145-2d7f-4a24-b35f-04b0cd240264)

- Quickly review user behavior by sorting records chronologically and observing the **Potential Activity** field.

![imagen](https://github.com/user-attachments/assets/fe83af66-cc8f-4290-8b9b-540c491f33a9)

  and applying filters as needed.
  

![imagen](https://github.com/user-attachments/assets/db3c666d-f886-4513-a7b7-ee7c3810532a)
  

![imagen](https://github.com/user-attachments/assets/0dca8f1e-cafb-44ce-85e0-ae70f752f57b)



- Use the search bar to perform simple keyword filtering or **advanced regular expression (RegExp)** searches across all fields.


![imagen](https://github.com/user-attachments/assets/435882ed-ab08-4838-ab7b-82d35d2861f6)
![imagen](https://github.com/user-attachments/assets/53f3262e-ddb3-4749-a170-571f27a29823)

- Visualize and explore data from browser artifacts such as browsing history, downloads, bookmarks, and autofill entries. Browsing history is automatically categorized and tagged based on potential user activity, helping to identify relevant patterns and behaviors.

  History

  ![imagen](https://github.com/user-attachments/assets/5444bbc6-4ccd-452f-81bc-d386b8edcafa)

  Downloads

  ![imagen](https://github.com/user-attachments/assets/14a269fd-c90c-4153-9a3a-394f9b6ea897)

  Bookmarks

  ![imagen](https://github.com/user-attachments/assets/753dae17-b663-4420-a3df-64dc937cfa08)

  




  
