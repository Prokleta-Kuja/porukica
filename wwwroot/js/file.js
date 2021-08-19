let chunkMin = 1024 * 512; // 0.5 MB
let chunkSize = 1024 * 1024 * 10; // MB
let chunkMax = 30000000; // Kestrel default max ~28.6MB
let speedMin = 500;
let speedMax = 1500;
let canceled = false;

export async function start(dotnet, key) {
    canceled = false;
    let el = document.getElementById("upload-file");
    if (!el || !el.files || el.files.length === 0)
        return;

    let start = 0;
    let file = el.files[0];

    do {
        let elapsedStart = performance.now();
        let end = Math.min(start + chunkSize, file.size);

        var headers = new Headers();
        headers.append("Content-Type", file.type);
        headers.append("key", key);
        var req = new Request("/upload", {
            method: "POST",
            headers: headers,
            body: file.slice(start, end, file.type),
        });

        try {
            await fetch(req);
            start = end;

            if (!canceled) {
                let completed = (end / file.size) * 100;
                dotnet.invokeMethodAsync('UpdateProgress', key, completed);
            }
        }
        catch (e) {
            console.error(e);
            canceled = true;
        }

        let elapsed = performance.now() - elapsedStart;

        if (elapsed > speedMax) {
            // make chunk smaller
            let half = Math.round(chunkSize / 2);
            chunkSize = half < chunkMin ? chunkMin : half;
        }
        else if (elapsed < speedMin) {
            // make chunk larger
            let double = chunkSize * 2;
            chunkSize = double > chunkMax ? chunkMax : double;
        }
    } while (!canceled && start !== file.size);
}

export function cancel() { canceled = true; }