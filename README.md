# netics-1

Untuk membuat workflow CI-CD, kita bisa menggunakan GithubActions. Tetapi sebelum itu, pada project ini terdapat applikasi .NET yang akan memberikan response API pada endpoint /health.

    ```C#
    using dotenv.net;
    using System.Text.Json;

    namespace NeticsCICD
    {
        public static class Program
        {
            private static IDictionary<string, string> env = new Dictionary<string, string>();
            private static DateTime startTime;

            public static void Main(string[] args)
            {
                startTime = DateTime.Now;

                DotEnv.Load(new DotEnvOptions(
                    envFilePaths: new string[]
                    {
                        ".env"
                    }
                ));
                env = DotEnv.Read();

                var builder = WebApplication.CreateBuilder(args);
                builder.WebHost.UseUrls("http://0.0.0.0:8080");
                var app = builder.Build();

                app.MapGet("/", Index);
                app.MapGet("/health", Health);

                app.Run();
            }
            public static JsonDocument Health()
            {
                Dictionary<string, object> response = new Dictionary<string, object>();
                response.Add("nama", env["NAMA"]);
                response.Add("nrp", env["NRP"]);
                response.Add("status", "UP");
                response.Add("timestamp", DateTime.Now.ToString());;
                response.Add("uptime", (DateTime.Now - startTime).ToString());

                return JsonSerializer.SerializeToDocument(response);
            }
            public static IResult Index()
            {
                using(StreamReader file = File.OpenText("./public/index.html"))
                {
                    string data = file.ReadToEnd();
                    return Results.Content(data, "text/html");
                }
            }
        }
    }
    ```

Lalu untuk menjalankan applikasi ini, juga menggunakan Docker. Dalam hal ini, applikasi harus dicompile terlebih dahulu sebelum melakukan build image dari Dockerfile. Karena, pada saat kita membuild docker image, applikasi yang sudah di build sebelumnya hanya di copy dan dijalankan saja tanpa melakukan proses build lagi.

    ```Dockerfile
    FROM ubuntu:22.04

    WORKDIR /var/www/neticscicd

    RUN apt update -y
    RUN apt install libicu70 -y

    COPY ./publish/ ./
    EXPOSE 8080

    ENTRYPOINT ["./App"]
    ```

Selanjutnya untuk membuat proses automasi dari build application, build docker image, dan deploymentnya kita akan menggunakan GithubActions dengan membuat workflownya pada folder .github/workflows/ pada root directory project Github. Dalam hal ini adalah .github/workflows/ci-cd.yml. Struktur dari workflow CI-CD yang akan dibuat adalah sebagai berikut.

    ```yml
    name: {NAMA}
    run-name: {RUNNING NAME}
    on: {EVENT}

    env:
        {NAME}: {VALUE}

    jobs:
        {JOB_NAME}:
            {}
        {JOB_NAME}:
            runs-on: {RUNNER}
            steps:
                - {STEPS}
        ...
    ```

Bagian pertama adalah bagian informasi umum, mengenai nama dari workflow, kapan workflow tersebut dijalankan, serta variabel tambahan lainnya seperti env.

    ```yml
    name: {NAMA}
    run-name: {RUNNING NAME}
    on: {EVENT}

    env:
        {NAME}: {VALUE}
    ```

Pada bagian `on: {EVENT}` merupakan bagian yang akan menunggu sebuah `Event` untuk diberikan agar workflow bisa dijalankan. Event tersebut bisa berupa saat kita melakukan push, melakukan pull request, dan lainnya. Event tersebut juga bisa merupakan Event yang diberikan secara manual melalui User Interface Web Browser, ataupun CLI. Dalam hal project ini, event yang digunakan adalah event manual yaitu `workflow_dispatch`. Dengan menggunakan workflow_dispatch, kita juga bisa menambahkan beberapa input yang bisa didapatkan pada interface web browser. Dalam hal ini input yang diminta bisa berupa Environment apa yang akan digunakan. Untuk mendeklarasikan input bisa menggunakan

    ```yml
    on:
      workflow_dispatch:  
        inputs:
        {NAMA_VARIABEL}:
          type: {TIPE_VARIABEL}
          description: {DESKRIPSI}
        {NAMA_VARIABEL}:
          type: {TIPE_VARIABEL}
          description: {DESKRIPSI}
        ...
    ```
    
    Dalam hal project ini, akan membutuhkan sebuah input berupa type `environment` untuk memberikan kontrol terhadap build dan deploy otomatis terhadap berbagai environment. Sehingga hasilnya menjadi seperti ini

    ```yml
    name: build-deploy-application
    run-name: application-builder-deployer-${{ github.actor }}
    on:
      workflow_dispatch:  
        inputs:
        environment:
          type: environment
          description: Select Build & Deploy Environment
    ```

    Lalu terdapat bagian `env`. Bagian ini bisa digunakan untuk mendeklarasikan variabel-variabel. Dalam hal ini, yang dibutuhkan hanya melakukan deklarasi variabel berupa versi SDK dari .NET.

    ```yml
    env:
        DOTNET_SDK_VERSION: 8.x.x
    ```

    Selanjutnya adalah `jobs` atau pekerjaan yang akan dieksekusi pada workflow. Pada bagian ini, kita bisa membuat beberapa pekerjaan yang dimana pekerjaan tersebut bisa dijalankan secara parallel, atau beberapa pekerjaan menunggu pekerjaan lainnya selesai sebelum dijalankan. Untuk mendeklrasikan `jobs`, adalah sebagai berikut

    ```yml
    jobs:
        {JOB_NAME}:
            {}
        {JOB_NAME}:
            runs-on: {RUNNER}
            steps:
                - {STEPS}
        ...
    ```

