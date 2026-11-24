package com.utlport.launcher;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;

import java.io.BufferedInputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.util.zip.ZipEntry;
import java.util.zip.ZipInputStream;

/**
 * Fragment headless que abre o seletor nativo do Android (ACTION_OPEN_DOCUMENT)
 * sem precisar de permissoes de armazenamento nem editar o AndroidManifest.xml.
 */
public class UTLZipPickerFragment extends Fragment {
    private static final int REQ_PICK_ZIP = 49211;
    private static final String FRAG_TAG = "UTLZipPickerFragmentTag";

    private String targetDir;
    private String unityObjectName;

    public static void openZipPicker(final String destDir, final String callbackGo) {
        final Activity act = UnityPlayer.currentActivity;
        if (act == null) return;
        act.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    FragmentManager fm = act.getFragmentManager();
                    Fragment old = fm.findFragmentByTag(FRAG_TAG);
                    if (old != null) {
                        fm.beginTransaction().remove(old).commitAllowingStateLoss();
                    }
                    UTLZipPickerFragment frag = new UTLZipPickerFragment();
                    Bundle args = new Bundle();
                    args.putString("destDir", destDir);
                    args.putString("callbackGo", callbackGo);
                    frag.setArguments(args);
                    fm.beginTransaction().add(frag, FRAG_TAG).commitAllowingStateLoss();
                } catch (Exception e) {
                    UnityPlayer.UnitySendMessage(callbackGo, "OnNativeExtractError", "Falha ao abrir seletor: " + e.getMessage());
                }
            }
        });
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (getArguments() != null) {
            targetDir = getArguments().getString("destDir");
            unityObjectName = getArguments().getString("callbackGo");
        }
        try {
            Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.setType("*/*");
            String[] mimes = new String[] { "application/zip", "application/x-zip-compressed", "application/octet-stream" };
            intent.putExtra(Intent.EXTRA_MIME_TYPES, mimes);
            startActivityForResult(intent, REQ_PICK_ZIP);
        } catch (Exception e) {
            send("OnNativeExtractError", "Erro no Intent: " + e.getMessage());
            detachSelf();
        }
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQ_PICK_ZIP) return;

        if (resultCode != Activity.RESULT_OK || data == null || data.getData() == null) {
            send("OnNativeExtractCancelled", "");
            detachSelf();
            return;
        }

        final Uri uri = data.getData();
        final Activity act = getActivity();
        final String dest = targetDir;

        send("OnNativeExtractProgress", "0|Lendo arquivo ZIP selecionado...");

        new Thread(new Runnable() {
            @Override
            public void run() {
                InputStream raw = null;
                ZipInputStream zis = null;
                try {
                    File outRoot = new File(dest);
                    if (!outRoot.exists()) outRoot.mkdirs();

                    raw = act.getContentResolver().openInputStream(uri);
                    if (raw == null) {
                        send("OnNativeExtractError", "Nao foi possivel abrir o arquivo selecionado.");
                        return;
                    }
                    zis = new ZipInputStream(new BufferedInputStream(raw, 65536));
                    byte[] buf = new byte[65536];
                    int count = 0;
                    ZipEntry entry;
                    while ((entry = zis.getNextEntry()) != null) {
                        if (entry.isDirectory()) {
                            zis.closeEntry();
                            continue;
                        }
                        String name = new File(entry.getName()).getName();
                        if (name == null || name.length() == 0 || name.endsWith(".meta")) {
                            zis.closeEntry();
                            continue;
                        }
                        File outFile = new File(outRoot, name);
                        FileOutputStream fos = new FileOutputStream(outFile);
                        try {
                            int n;
                            while ((n = zis.read(buf)) > 0) {
                                fos.write(buf, 0, n);
                            }
                        } finally {
                            fos.close();
                        }
                        zis.closeEntry();
                        count++;
                        if (count <= 3 || count % 5 == 0) {
                            send("OnNativeExtractProgress", count + "|Extraindo (" + count + "): " + name);
                        }
                    }
                    send("OnNativeExtractDone", String.valueOf(count));
                } catch (Exception e) {
                    send("OnNativeExtractError", "Erro na extracao: " + e.getMessage());
                } finally {
                    try { if (zis != null) zis.close(); } catch (Exception ignored) {}
                    try { if (raw != null) raw.close(); } catch (Exception ignored) {}
                    detachSelf();
                }
            }
        }).start();
    }

    private void send(String method, String msg) {
        if (unityObjectName != null && unityObjectName.length() > 0) {
            UnityPlayer.UnitySendMessage(unityObjectName, method, msg == null ? "" : msg);
        }
    }

    private void detachSelf() {
        final Activity act = getActivity();
        if (act == null) return;
        act.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    FragmentManager fm = act.getFragmentManager();
                    fm.beginTransaction().remove(UTLZipPickerFragment.this).commitAllowingStateLoss();
                } catch (Exception ignored) {}
            }
        });
    }
}
