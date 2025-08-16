package com.example.viewer.file

import android.os.Bundle
import android.view.View
import android.widget.Button
import androidx.activity.result.contract.ActivityResultContracts
import androidx.fragment.app.Fragment
import com.example.viewer.R

class ImportPmxFragment : Fragment(R.layout.panel_file) {
    private val openDocument = registerForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        uri?.let {
            // TODO: implement PMX loading
        }
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        view.findViewById<Button>(R.id.button_import_pmx).setOnClickListener {
            openDocument.launch(arrayOf("*/*"))
        }
    }
}
