Imports System.IO
Imports System.Xml

Module Program

    Private filenamearray As New Hashtable From {{"A", "Audi"}, {"C", "Skoda"}, {"L", "Lnf"}, {"P", "Porsche"}, {"S", "Seat"}, {"V", "Volkwagen"}}
    Private header(1)() As String
    Private logFile As String
    Private input As String
    Private output As String
    Private Const cstUSAGE As String = "Usage: tagestab.exe <input file> <output folder>"

    Sub Main(args As String())

        If Init() <> 0 Then Exit Sub

        If args.Count = 2 Then
            If File.Exists(args(0)) AndAlso Directory.Exists(args(1)) Then
                input = args(0)
                output = args(1)
                ProcessFile(input)
            End If
        Else
            Console.WriteLine("Ivalid argumets specified.")
            Console.WriteLine()
            Console.WriteLine(cstUSAGE)
        End If
    End Sub

    Private Function Init() As Integer

        Dim status As Integer = 0

        logFile = "log.txt"

        If Not File.Exists(logFile) Then
            File.Create(logFile)
        End If

        status = LoadHeader()

        Return status

    End Function

    Private Sub ProcessFile(path As String)
        Dim fileHeader As String()
        Dim stpw As New Stopwatch
        Dim line As String()
        Dim Q As Queue(Of String()) = New Queue(Of String())

        For Each f As String In Directory.GetFiles(output)
            File.Delete(f)
        Next

        stpw.Restart()

        Using sr As New StreamReader(input)

            'read file header
            fileHeader = sr.ReadLine().Split(",")

            ' if file columns < header exit
            If fileHeader.Length < header(0).Length Then Exit Sub

            While Not sr.EndOfStream
                line = sr.ReadLine.Split(""",""")

                For i As Integer = 0 To header(0).Length - 1

                    line(i) = line(i).Replace("""", "").Replace(";", ".")
                    'FZGPRNR: add 0
                    If i = 20 AndAlso line(i).Length = 6 Then line(i) = $"0{line(i)}"

                    Select Case header(2)(i)
                        Case "date"
                            line(i) = line(i).Split(" ")(0)
                        Case "number"
                            If line(i).Length <> 0 Then
                                If line(i)(0) = "." Then line(i) = $"0{line(i)}"
                                line(i) = Double.Parse(line(i), Globalization.CultureInfo.InvariantCulture)
                            End If
                            line(i) = line(i).Replace("""", "")
                        Case Else
                            line(i) = $"""{line(i)}"""
                    End Select

                Next

                Q.Enqueue(line)

                If Q.Count > 5000 Then
                    WriteCSV(Q)
                    Q.Clear()
                End If



            End While

            WriteCSV(Q)

        End Using

        stpw.Stop()
        Log($"Data proccessed in {stpw.ElapsedMilliseconds} ms")


    End Sub

    Private Sub WriteCSV(q As Queue(Of String()))

        Dim tasks As New List(Of Task)
        Dim destStreams As New List(Of StreamWriter)

        Try
            For Each s As String In filenamearray.Keys
                Dim arr As String()() = q.Where(Function(x As String()) x(2) = """" & s & """").ToArray

                WriteCSV(arr)

            Next

        Catch ex As Exception

        End Try

    End Sub


    Private Sub WriteCSV(line As String()())
        If line.Length > 0 Then
            Dim brand As String = line(0)(2).Replace("""", "")

            Dim sep As String = ";"
            Dim filename As String = filenamearray(brand)

            Using sw As New StreamWriter($"{output}\{filename}.csv", True)
                For Each l As String() In line
                    sw.WriteLine(String.Join(sep, l))
                Next

            End Using
        End If


    End Sub

    ''' <summary>
    ''' Log to file 'log.txt'
    ''' </summary>
    ''' <param name="s">String to write to file</param>
    Sub Log(s As String)
        Dim sw As New StreamWriter("log.txt", True)
        sw.WriteLine($"[{Date.Now}] - {s}")
        sw.Close()
    End Sub

    ''' <summary>
    ''' Load header from XML
    ''' </summary>
    ''' <param name="path">Path to file containing header data. Default "header.xml"</param>
    Private Function LoadHeader(Optional path As String = "header.xml") As Integer

        'Check if file exists
        If Not File.Exists(path) Then
            Log($"File not found: {path}")
            'Throw New FileNotFoundException
            Return 1
        End If

        Dim name As New List(Of String)
        Dim altname As New List(Of String)
        Dim type As New List(Of String)

        Dim doc As New XmlDocument
        Dim nl As XmlNodeList

        doc.Load(path)

        nl = doc.SelectNodes("/header/h")

        For i As Integer = 0 To nl.Count - 1
            name.Add(nl(i).Attributes.GetNamedItem("name").Value)
            altname.Add(nl(i).Attributes.GetNamedItem("altname").Value)
            type.Add(nl(i).Attributes.GetNamedItem("type").Value)
        Next

        header = {name.ToArray, altname.ToArray, type.ToArray}
        Return 0
    End Function
End Module
