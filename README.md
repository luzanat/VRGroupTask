
# Solution Description

1. I am using FileSystemWatcher to monitor a FilesToProcess folder (in the same folder where exe is located, will be created if not exists) for new files. In this case it won't process files that are already in the folder. Potentially it may cause problems if for some reason program won't work for some time, it is better to check for existing files and move/delete processed files from the folder.
2. New files are added to the queue. It will start a thread which will work while there are files in the queue.
3. Files are parsed line by line.
4. Parsed records are being saved to SQL Server database in batches using SqlBulkCopy. I made that Box and all its content will be saved in one transaction, so batch size can be slightly larger to make sure that all box content is parsed. Connection string is located in DataAccess.cs.
5. SqlBulkCopy saves records to the temp table, then these records will be merged into the main table using MERGE sql script. I added this to avoid primary key violation error, but it is decreasing performance. 

I am assuming that in a ASN file every box has unique Identifier field and that every box can contain only one ISBN. If it is not then validation must be added (e.g. using dictionaries to check for duplicate ids and increasing quantity if there are more than one ISBN in the box).

# Task Description 
Alongside this challenge, you will also find a [sample data file](data.txt), which is a fixed-length text file.

This is a sample of an ASN (Acknowledgement Shipping Notification) message that we receive from one of our suppliers.
Each HDR section, represents a box, and the lines below the HDR section describe the contents of the box.
When we reach another HDR section, it means that there is another box and we repeat the process from the beginning.

## Data file structure
<pre>
HDR  TRSP117                                           6874454I                           
LINE P000001661         9781465121550         12     
LINE P000001661         9925151267712         2      
LINE P000001661         9651216865465         1      
</pre>

## Description
<pre>
HDR             - Just a keyword telling that a new box is being described.
TRSP117         - Supplier identifier.
6874454I        - Carton box identifier. Displayed on the box.
LINE            - Keyword to identify product item in the box.
P000001661      - Our PO Number that we sent to the supplier.
9781465121550   - ISBN 13 (product barcode).
12              - Product quantity.
</pre>

The solution should monitor a specific file path, and whenever a file is dropped in that folder, the file should be parsed and loaded into a database.
The file could be very large and exceed the available RAM.

IMPORTANT: Your submission will be verified for correctness, and also used to evaluate your approach, attention to detail, and craftmanship. 
This is your opportunity to give us an idea of what we can expect from you on a day-to-day basis. Try to write the code as you would any ticket assigned to you. 
We understand this is an effort you do in your free time, and we do not expect a fully polished production ready product. It's perfectly fine to take shortcuts and omit code 
in case of time contraints. Just drop a comment where you would have implemented code, and explain what approach you would take and what the code would have done.

Because it's an assignment for a senior position, we'd ask you to implement at least one advanced approach in the code, such as memory optimization, efficient bulk insertion strategies, or similar.

    The following code class is merely an example to demonstrate the structure. You should modify it as needed to achieve your goal.
    If you have any question about the task, please feel free to ask.

```csharp
public class Box
{
    public string SupplierIdentifier { get; set; }
    public string Identifier { get; set; }

    public IReadOnlyCollection<Content> Contents { get; set; } 

    public class Content
    {
        public string PoNumber { get; set; }
        public string Isbn { get; set; }
        public int Quantity { get; set; }
    }
}
```
We look forward to seeing your solution!