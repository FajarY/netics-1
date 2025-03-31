# netics-1

Untuk membuat workflow CI-CD, kita bisa menggunakan GithubActions. Tetapi sebelum itu, pada project ini terdapat applikasi .NET yang akan memberikan response API pada endpoint /health.

[http://cryothink.com:8080](http://cryothink.com:8080)

[http://cryothink.com:8080/health](http://cryothink.com:8080/health)

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

Dalam hal project ini, akan membutuhkan sebuah input berupa type `environment` untuk memberikan kontrol terhadap build dan deploy otomatis terhadap berbagai environment. Selain itu untuk melakukan pengaksesan variabel bisa menggunakan ${{ {ACTION} }}. Sehingga hasilnya menjadi seperti ini

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

Sebuah job bisa berisi runner yang menjalankannya, environment yang digunakan, serta langkah langkah / `steps` dari job tersebut. Steps ini berisi perintah command yang akan dieksekusi pada runner.

```yml
jobs:
    {JOB_NAME}:
        environment: 
          name: {ENVIRONMENT_NAME}
        runs-on: {RUNNER}
        steps:
            - {STEPS}
    ...
```

Command tersebut bisa sebuah command shell, ataupun bisa menggunakan template. Template bisa kita gunakan dengan memanggil workflow pada sebuah repositori. Jika kita menggunakan Command secara manual, bisa menggunakan `run: {COMMAND}`, dan jika kita menggunakan template gunakan `uses: {REPOSITORY TEMPLATE}`. Setelah itu kita juga bisa menambahkan variabel yang digunakan pada step tersebut dengan `with: ...`. Step juga bisa diberi nama, untuk memberikan tambahan informasi atau hanya sebagai penanda dengan `name: {NAMA}`. Selain itu kita juga bisa menambahkan working directory dengan `working-directory: {DIR}`.

Sekarang kita akan membuat pekerjaan yang melakukan build dari applikasi & docker image serta mengupload docker image tersebut pada docker hub. Environment yang digunakan akan disesuaikan dengan input dari interface workflow, serta runner adalah `ubuntu-22.04` Sehingga job akan terlihat seperti ini
    
```yml
jobs:
build:
    environment: 
      name: ${{ inputs.environment }}
    runs-on: ubuntu-22.04
    steps:
        ...
```

Pertama kita akan melakukan setup dari runnernya, setup bisa berupa melakukan clonning dari repo dengan menggunakan template `actions/checkout@v4`, lalu download package yang digunakan untuk membuild applikasi, docker image, dan credentialsnya. Untuk membuild applikasi karena kita menggunakan .NET maka gunakan `actions/setup-dotnet@v4`, untuk membuild docker image menggunakan `docker/setup-buildx-action@v3`, untuk melakukan login pada docker hub gunakan `docker/login-action@v3`. Sebelum itu, kita juga bisa mendapatkan nilai variabel dari secrets environment dengan mengaksesnya dengan `${{ env.{NAMA_SECRET} }}`. Sehingga step menjadi seperti ini

```yml
steps:
    - uses: actions/checkout@v4
    - name: Setting up .NET ${{ env.DOTNET_SDK_VERSION }}
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_SDK_VERSION }}
        cache: true
        cache-dependency-path: ./App/packages.lock.json
    - name: Setting up Docker Buildx
      uses: docker/setup-buildx-action@v3
    - name: Inserting Docker Hub Credentials
      uses: docker/login-action@v3
      with:
        username: ${{ secrets.DOCKER_HUB_USERNAME }}
        password: ${{ secrets.DOCKER_HUB_PASSWORD }}
```

Bisa dilihat juga menambahkan variabel yang digunakan pada beberapa template. Pada `actions/setup-dotnet@v4` kita juga melakukan caching dengan targetnya adalah `package.lock.json`. Hal ini dilakukan agar kita tidak perlu melakukan banyak download package yang dilakukan pada `dotnet restore`, jika package tersebut telah ada dan tidak berubah. Pada `docker/login-action@v3` kita memberikan username dan password yang berasal dari secret.

Sekarang kita akan melakukan build applikasinya, tetapi sebelum itu kita juga akan membuat .env dengan melakukan echo variabel pada secret, mengingat .env tidak terdapat langsung pada repositori karena bersifat rahasia.

```yml
- name: Fetching .env
    working-directory: ./App
    run: |
      echo "NAMA=${{ secrets.APP_NAMA }}" >> .env
      echo "NRP=${{ secrets.APP_NRP }}" >> .env
```

Pada .NET untuk melakukan build applikasi, kita perlu menginstall dependency menggunakan `dotnet restore` dan untuk melakukan build applikasi gunakan `dotnet publish`. `dotnet publish` akan digabungkan pada sebuah script yang berisi pembersihan build, dan pemindahan build agar bisa digunakan pada membuild docker image.

```yml
- name: Installing dependencies
  working-directory: ./App
  run: dotnet restore --locked-mode
- name: Compiling application
  working-directory: ./App
  run: ./compile.sh
- name: Checking compiled application
  working-directory: ./Dockerfile/publish
  run: ls -al
```

Dan akhirnya build docker image dan menguploadnya ke docker hub bisa dilakukan. Dengan menggunakan template `docker/build-push-action@v6`.

```yml
- name: Building docker image
  uses: docker/build-push-action@v6
  with:
    context: ./Dockerfile
    push: true
    tags: fajary/netics-1:latest
```

Sehingga seluruh job build applikasi adalah sebagai berikut.

```yml
build:
    environment: 
      name: ${{ inputs.environment }}
    runs-on: ubuntu-22.04
    steps:
        - uses: actions/checkout@v4
        - name: Setting up .NET ${{ env.DOTNET_SDK_VERSION }}
          uses: actions/setup-dotnet@v4
          with:
            dotnet-version: ${{ env.DOTNET_SDK_VERSION }}
            cache: true
            cache-dependency-path: ./App/packages.lock.json
        - name: Setting up Docker Buildx
          uses: docker/setup-buildx-action@v3
        - name: Inserting Docker Hub Credentials
          uses: docker/login-action@v3
          with:
            username: ${{ secrets.DOCKER_HUB_USERNAME }}
            password: ${{ secrets.DOCKER_HUB_PASSWORD }}
        - name: Fetching .env
          working-directory: ./App
          run: |
            echo "NAMA=${{ secrets.APP_NAMA }}" >> .env
            echo "NRP=${{ secrets.APP_NRP }}" >> .env
        - name: Installing dependencies
          working-directory: ./App
          run: dotnet restore --locked-mode
        - name: Compiling application
          working-directory: ./App
          run: ./compile.sh
        - name: Checking compiled application
          working-directory: ./Dockerfile/publish
          run: ls -al
        - name: Building docker image
          uses: docker/build-push-action@v6
          with:
            context: ./Dockerfile
            push: true
            tags: fajary/netics-1:latest
```

Setelah melakukan build, kita akan melakukan deployment dari applikasi.

```yml
deploy:
    environment: 
      name: ${{ inputs.environment }}
    needs: build
    runs-on: ubuntu-22.04
    steps:
        ...
```

Pertama akan melakukan clone repository, karena terdapat script yang bisa membantu pendeployan.

```yml
- uses: actions/checkout@v4
```

Lalu, untuk deployment sendiri, kita akan menggunakan ssh untuk mengakses server kita, dengan `ssh -i {KEY_FILE} {USER}@{ADDRESS} -p {PORT} {COMMAND/APPLICATION}`. Kita akan memberikan beberapa command seperti pull docker image, dan run container dengan meng-cat command tersebut dan piping pada ssh. Sehingga stepnya akan menjadi seperti ini

```yml
- name: Connecting to remote PC & Deploy
  working-directory: ./App
  run: |
    echo "${{ secrets.REMOTE_SSH_KEY }}" > Deploy.pem
    chmod 400 Deploy.pem
    echo "sudo docker container run -d -p ${{secrets.SERVER_PORT}}:8080 --name netics-1 fajary/netics-1:latest" >> run.sh
    cat run.sh
    cat run.sh | ssh -o "StrictHostKeyChecking no" -i Deploy.pem ${{ secrets.REMOTE_SSH_USERNAME }}@${{ secrets.REMOTE_SSH_HOST }} -p ${{ secrets.REMOTE_SSH_PORT }} /bin/bash
```